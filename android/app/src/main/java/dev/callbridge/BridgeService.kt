package dev.callbridge

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.telephony.TelephonyManager
import org.json.JSONObject

private const val CHANNEL_ID = "bridge"
private const val NOTIFICATION_ID = 1
private const val RECONNECT_DELAY_MS = 3000L

/** Keeps the connection to the PC alive and forwards call audio and control. */
class BridgeService : Service(), BridgeConnection.Listener {
	private val main = Handler(Looper.getMainLooper())
	private lateinit var calls: CallController
	private var connection: BridgeConnection? = null
	private var capture: DownlinkCapture? = null
	private var pairing: Pairing? = null
	private var lastNumber: String? = null
	private var lastCall = "idle"
	private var stopping = false

	private val phoneReceiver = object : BroadcastReceiver() {
		override fun onReceive(context: Context, intent: Intent) {
			val number = intent.getStringExtra(TelephonyManager.EXTRA_INCOMING_NUMBER)
			when (intent.getStringExtra(TelephonyManager.EXTRA_STATE)) {
				TelephonyManager.EXTRA_STATE_RINGING -> onCallState("ringing", number)
				TelephonyManager.EXTRA_STATE_OFFHOOK -> onCallState("active", number)
				TelephonyManager.EXTRA_STATE_IDLE -> onCallState("idle", null)
			}
		}
	}

	override fun onCreate() {
		super.onCreate()
		calls = CallController(this)
		val channel = NotificationChannel(CHANNEL_ID, getString(R.string.notification_channel), NotificationManager.IMPORTANCE_LOW)
		getSystemService(NotificationManager::class.java).createNotificationChannel(channel)
		registerReceiver(phoneReceiver, IntentFilter(TelephonyManager.ACTION_PHONE_STATE_CHANGED), RECEIVER_EXPORTED)
	}

	override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
		if (intent?.action == ACTION_STOP) {
			stopping = true
			disconnect()
			BridgeState.running = false
			BridgeState.update("Disconnected")
			stopForeground(STOP_FOREGROUND_REMOVE)
			stopSelf()
			return START_NOT_STICKY
		}
		pairing = PairingStore(this).load()
		val current = pairing
		if (current == null) {
			stopSelf()
			return START_NOT_STICKY
		}
		stopping = false
		BridgeState.running = true
		val notification = Notification.Builder(this, CHANNEL_ID)
			.setSmallIcon(R.drawable.ic_launcher)
			.setContentTitle(getString(R.string.app_name))
			.setContentText("Bridge to ${current.name}")
			.setOngoing(true)
			.build()
		startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_MICROPHONE)
		connect()
		return START_REDELIVER_INTENT
	}

	private fun connect() {
		val current = pairing ?: return
		disconnect()
		BridgeState.update("Connecting to ${current.name}")
		connection = BridgeConnection(current, Build.MODEL, this).also { it.start() }
	}

	private fun disconnect() {
		capture?.stop()
		capture = null
		connection?.close()
		connection = null
	}

	override fun onConnected() {
		BridgeState.update("Connected to ${pairing?.name}")
		main.post {
			sendState(lastCall, lastNumber)
			if (lastCall == "active") startCapture()
		}
	}

	override fun onClosed(reason: String) {
		main.post {
			capture?.stop()
			capture = null
			if (stopping) return@post
			if (reason == "wrong code") {
				BridgeState.running = false
				BridgeState.update("Pairing is outdated, scan the QR code again")
				stopForeground(STOP_FOREGROUND_REMOVE)
				stopSelf()
				return@post
			}
			BridgeState.update("Disconnected ($reason), retrying")
			main.postDelayed({ if (!stopping) connect() }, RECONNECT_DELAY_MS)
		}
	}

	override fun onControl(message: JSONObject) {
		main.post {
			when (message.optString("t")) {
				"answer" -> calls.answer()
				"reject", "hangup" -> calls.hangup()
				"dial" -> calls.dial(message.optString("number"))
			}
		}
	}

	override fun onUplink(packet: ByteArray) {
	}

	private fun onCallState(state: String, number: String?) {
		main.post {
			if (number != null) lastNumber = number
			lastCall = state
			if (state == "idle") lastNumber = null
			sendState(state, lastNumber)
			if (state == "active") startCapture() else stopCapture()
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
		capture = DownlinkCapture(link::sendDownlink) { reason ->
			link.sendControl(JSONObject().put("t", "audio").put("route", "unavailable").put("reason", reason))
		}.also { it.start() }
	}

	private fun stopCapture() {
		capture?.stop()
		capture = null
	}

	override fun onDestroy() {
		unregisterReceiver(phoneReceiver)
		disconnect()
		super.onDestroy()
	}

	override fun onBind(intent: Intent?): IBinder? = null

	companion object {
		const val ACTION_STOP = "dev.callbridge.STOP"
	}
}
