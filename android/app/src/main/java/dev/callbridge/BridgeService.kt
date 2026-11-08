package dev.callbridge

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.ServiceInfo
import android.database.ContentObserver
import android.graphics.drawable.Icon
import android.provider.ContactsContract
import android.os.Build
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.telephony.TelephonyManager
import org.json.JSONObject

private const val CHANNEL_ID = "bridge"
private const val NOTIFICATION_ID = 1
private const val RECONNECT_DELAY_MS = 3000L
private const val NETWORK_RETRY_MS = 600L
private const val MAX_RETRY_MS = 15000L
private const val CONTACTS_DEBOUNCE_MS = 3000L

/** Keeps the connection to the PC alive and forwards call audio and control. */
class BridgeService : Service(), BridgeConnection.Listener {
	private val main = Handler(Looper.getMainLooper())
	private lateinit var calls: CallController
	private lateinit var muter: PhoneAudioMuter
	private lateinit var overlay: CallModeOverlay
	private var callMode = CallMode.PC
	private var connection: BridgeConnection? = null
	private var capture: DownlinkCapture? = null
	@Volatile private var uplink: UplinkPlayer? = null
	private var pairing: Pairing? = null
	private var lastNumber: String? = null
	private var lastCall = "idle"
	private var stopping = false
	private var retryPending = false
	private var failures = 0
	private var microphoneForeground = false
	private var wifiLock: android.net.wifi.WifiManager.WifiLock? = null
	private val reconnectTask = Runnable {
		retryPending = false
		if (!stopping) connect()
	}
	private var networkWatch: android.net.ConnectivityManager.NetworkCallback? = null

	private val sendContactsTask = Runnable { sendContacts() }

	private val contactsObserver = object : ContentObserver(main) {
		override fun onChange(selfChange: Boolean) {
			main.removeCallbacks(sendContactsTask)
			main.postDelayed(sendContactsTask, CONTACTS_DEBOUNCE_MS)
		}
	}

	private val phoneReceiver = object : BroadcastReceiver() {
		override fun onReceive(context: Context, intent: Intent) {
			val number = intent.getStringExtra(TelephonyManager.EXTRA_INCOMING_NUMBER)
			when (intent.getStringExtra(TelephonyManager.EXTRA_STATE)) {
				TelephonyManager.EXTRA_STATE_RINGING -> onCallState("ringing", number)
				TelephonyManager.EXTRA_STATE_OFFHOOK -> onCallState(if (CallRelay.holding) "held" else "active", number)
				TelephonyManager.EXTRA_STATE_IDLE -> onCallState("idle", null)
			}
		}
	}

	override fun onCreate() {
		super.onCreate()
		calls = CallController(this)
		muter = PhoneAudioMuter(this)
		muter.restore()
		overlay = CallModeOverlay(this, ::toggleCallMode)
		val channel = NotificationChannel(CHANNEL_ID, getString(R.string.notification_channel), NotificationManager.IMPORTANCE_LOW)
		getSystemService(NotificationManager::class.java).createNotificationChannel(channel)
		networkWatch = NetworkReport.watch(this) { main.post { onNetworkChanged() } }
		registerReceiver(phoneReceiver, IntentFilter(TelephonyManager.ACTION_PHONE_STATE_CHANGED), RECEIVER_EXPORTED)
		contentResolver.registerContentObserver(ContactsContract.Contacts.CONTENT_URI, true, contactsObserver)
		CallRelay.onHoldChanged = { onHoldChanged(it) }
		CallRelay.onHoldFailed = { main.post { holdFailed() } }
	}

	private fun holdFailed() {
		connection?.sendControl(JSONObject().put("t", "holdFailed"))
	}

	private fun onHoldChanged(holding: Boolean) {
		main.post {
			if (lastCall != "active" && lastCall != "held") return@post
			lastCall = if (holding) "held" else "active"
			sendState(lastCall, lastNumber)
			syncCall()
		}
	}

	override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
		if (intent?.action == ACTION_CONTACTS) {
			sendContacts()
			return START_NOT_STICKY
		}
		if (intent?.action == ACTION_TOGGLE_MODE) {
			toggleCallMode()
			return START_STICKY
		}
		if (intent?.action == ACTION_STOP) {
			stopping = true
			PairingStore(this).autostart = false
			main.removeCallbacks(reconnectTask)
			releaseWifi()
			disconnect()
			BridgeState.running = false
			BridgeState.update("")
			syncCall()
			stopForeground(STOP_FOREGROUND_REMOVE)
			stopSelf()
			return START_NOT_STICKY
		}
		val store = PairingStore(this)
		pairing = store.load()
		val current = pairing
		if (current == null) {
			stopSelf()
			return START_NOT_STICKY
		}
		stopping = false
		store.autostart = true
		recoverCallState()
		BridgeState.running = true
		enterForeground()
		holdWifi()
		main.removeCallbacks(reconnectTask)
		retryPending = false
		connect()
		return START_REDELIVER_INTENT
	}

	private fun recoverCallState() {
		if (lastCall != "idle") return
		lastCall = when (getSystemService(TelephonyManager::class.java).callState) {
			TelephonyManager.CALL_STATE_RINGING -> "ringing"
			TelephonyManager.CALL_STATE_OFFHOOK -> if (CallRelay.holding) "held" else "active"
			else -> "idle"
		}
	}

	private fun enterForeground() {
		val notification = buildNotification()
		try {
			startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_MICROPHONE)
			microphoneForeground = true
		} catch (e: RuntimeException) {
			android.util.Log.i("CallBridge", "microphone foreground refused (${e.message}), using the plain link type")
			microphoneForeground = false
			startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
		}
	}

	private fun promoteToMicrophone() {
		if (microphoneForeground || !BridgeState.running) return
		try {
			startForeground(NOTIFICATION_ID, buildNotification(), ServiceInfo.FOREGROUND_SERVICE_TYPE_MICROPHONE)
			microphoneForeground = true
		} catch (e: RuntimeException) {
			android.util.Log.w("CallBridge", "cannot switch to the microphone type: ${e.message}")
		}
	}

	@Suppress("DEPRECATION")
	private fun holdWifi() {
		if (wifiLock?.isHeld == true) return
		val manager = applicationContext.getSystemService(android.net.wifi.WifiManager::class.java)
		wifiLock = manager.createWifiLock(android.net.wifi.WifiManager.WIFI_MODE_FULL_HIGH_PERF, "callbridge:link").also {
			it.setReferenceCounted(false)
			it.acquire()
		}
	}

	private fun releaseWifi() {
		wifiLock?.let { if (it.isHeld) it.release() }
		wifiLock = null
	}

	private fun onNetworkChanged() {
		if (stopping || !BridgeState.running || !retryPending) return
		main.removeCallbacks(reconnectTask)
		main.postDelayed(reconnectTask, NETWORK_RETRY_MS)
	}

	private fun buildNotification(): Notification {
		val builder = Notification.Builder(this, CHANNEL_ID)
			.setSmallIcon(R.drawable.ic_notification)
			.setContentTitle(getString(R.string.app_name))
			.setContentText(getString(R.string.notification_text, pairing?.name.orEmpty()))
			.setOngoing(true)
		if (lastCall == "active" && BridgeState.connected) {
			val toggle = PendingIntent.getService(
				this, 0, Intent(this, BridgeService::class.java).setAction(ACTION_TOGGLE_MODE),
				PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
			)
			val label = getString(if (callMode == CallMode.PC) R.string.mode_move_to_phone else R.string.mode_move_to_pc)
			builder.addAction(Notification.Action.Builder(Icon.createWithResource(this, R.drawable.ic_notification), label, toggle).build())
		}
		return builder.build()
	}

	private fun toggleCallMode() {
		setCallMode(if (callMode == CallMode.PC) CallMode.PHONE else CallMode.PC)
	}

	private fun setCallMode(mode: String) {
		if (mode != CallMode.PC && mode != CallMode.PHONE) return
		callMode = mode
		syncCall()
	}

	private fun syncCall() {
		val inCall = lastCall == "active" && BridgeState.connected
		if (inCall && callMode == CallMode.PC) startCapture() else stopCapture()
		if (inCall) overlay.show(callMode) else overlay.hide()
		if (BridgeState.running) getSystemService(NotificationManager::class.java).notify(NOTIFICATION_ID, buildNotification())
		connection?.sendControl(JSONObject().put("t", "mode").put("mode", callMode))
	}

	private fun connect() {
		val current = pairing ?: return
		disconnect()
		BridgeState.update(getString(R.string.status_connecting, current.name))
		val lan = LanNetwork.wifi(this)
		android.util.Log.i("CallBridge", "connecting: ${NetworkReport.snapshot(this, lan)}")
		connection =BridgeConnection(current, lan, LanNetwork.broadcast(this, lan), Build.MODEL, this).also { it.start() }
	}

	private fun disconnect() {
		stopCapture()
		connection?.close()
		connection = null
	}

	override fun onHostFound(host: String) {
		val current = pairing ?: return
		val updated = current.copy(hosts = (listOf(host) + current.hosts.filter { it != host }).take(6))
		PairingStore(this).save(updated)
		pairing = updated
	}

	override fun onConnected() {
		android.util.Log.i("CallBridge", "connected: ${NetworkReport.snapshot(this, null)}")
		failures = 0
		BridgeState.update(pairing?.name.orEmpty(), connected = true)
		main.post {
			sendState(lastCall, lastNumber)
			syncCall()
		}
		sendContacts()
	}

	private fun sendContacts() {
		val link = connection ?: return
		Thread {
			link.sendControl(JSONObject().put("t", "contacts").put("items", calls.contacts()))
		}.start()
	}

	override fun onClosed(reason: String) {
		main.post {
			stopCapture()
			if (stopping) return@post
			if (reason == "wrong code") {
				BridgeState.running = false
				PairingStore(this).autostart = false
				releaseWifi()
				BridgeState.update(getString(R.string.status_outdated))
				overlay.hide()
				stopForeground(STOP_FOREGROUND_REMOVE)
				stopSelf()
				return@post
			}
			BridgeState.update(getString(R.string.status_retry, if (reason == "PC not reachable") getString(R.string.reason_unreachable) else reason))
			syncCall()
			failures++
			retryPending = true
			main.removeCallbacks(reconnectTask)
			main.postDelayed(reconnectTask, minOf(RECONNECT_DELAY_MS * failures, MAX_RETRY_MS))
		}
	}

	override fun onControl(message: JSONObject) {
		main.post {
			when (message.optString("t")) {
				"answer" -> calls.answer()
				"reject", "hangup" -> calls.hangup()
				"dial" -> calls.dial(message.optString("number"))
				"contacts" -> sendContacts()
				"mode" -> setCallMode(message.optString("mode"))
				"hold" -> if (!CallRelay.hold(message.optBoolean("on", true))) holdFailed()
				"dtmf" -> Dtmf.parse(message.optString("digit"))?.let { CallRelay.dtmf(it) }
			}
		}
	}

	override fun onUplink(packet: ByteArray) {
		uplink?.push(packet)
	}

	private fun onCallState(state: String, number: String?) {
		main.post {
			if (number != null) lastNumber = number
			lastCall = state
			if (state == "idle") {
				lastNumber = null
				callMode = CallMode.PC
			}
			sendState(state, lastNumber)
			syncCall()
		}
	}

	private fun sendState(state: String, number: String?) {
		val message = JSONObject().put("t", "state").put("call", state)
		if (number != null) {
			message.put("number", number)
			calls.contactName(number)?.let { message.put("name", it) }
		}
		connection?.sendControl(message)
	}

	private fun startCapture() {
		if (capture != null) return
		val link = connection ?: return
		promoteToMicrophone()
		capture = DownlinkCapture(link::sendDownlink) { reason ->
			link.sendControl(JSONObject().put("t", "audio").put("route", "unavailable").put("reason", reason))
		}.also { it.start() }
		uplink = UplinkPlayer(this) { reason ->
			link.sendControl(JSONObject().put("t", "audio").put("route", "unavailable").put("reason", reason))
		}.also { it.start() }
		muter.mute()
	}

	private fun stopCapture() {
		if (capture == null) return
		capture?.stop()
		capture = null
		uplink?.stop()
		uplink = null
		muter.restore()
	}

	override fun onDestroy() {
		contentResolver.unregisterContentObserver(contactsObserver)
		networkWatch?.let { NetworkReport.unwatch(this, it) }
		CallRelay.onHoldChanged = null
		CallRelay.onHoldFailed = null
		main.removeCallbacks(reconnectTask)
		releaseWifi()
		unregisterReceiver(phoneReceiver)
		disconnect()
		overlay.hide()
		super.onDestroy()
	}

	override fun onBind(intent: Intent?): IBinder? = null

	companion object {
		const val ACTION_STOP = "dev.callbridge.STOP"
		const val ACTION_CONTACTS = "dev.callbridge.CONTACTS"
		const val ACTION_TOGGLE_MODE = "dev.callbridge.TOGGLE_MODE"
	}
}
