package dev.callbridge

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Bundle
import android.provider.Settings
import android.transition.TransitionManager
import android.view.View
import android.view.ViewGroup
import android.view.WindowInsets
import android.widget.Button
import android.widget.ImageView
import android.widget.TextView

private val PERMISSIONS = arrayOf(
	Manifest.permission.RECORD_AUDIO,
	Manifest.permission.READ_PHONE_STATE,
	Manifest.permission.READ_CALL_LOG,
	Manifest.permission.READ_CONTACTS,
	Manifest.permission.ANSWER_PHONE_CALLS,
	Manifest.permission.CALL_PHONE,
	Manifest.permission.POST_NOTIFICATIONS,
)

private const val REQUEST_ACCESS = 1
private const val REQUEST_SCAN = 2
private const val KEY_ASKED = "asked_for_access"

class MainActivity : Activity() {
	private lateinit var content: ViewGroup
	private lateinit var badge: View
	private lateinit var icon: ImageView
	private lateinit var title: TextView
	private lateinit var detail: TextView
	private lateinit var primary: Button
	private lateinit var accessHeader: View
	private lateinit var accessSection: View
	private lateinit var accessPhone: View
	private lateinit var accessDivider: View
	private lateinit var accessOverlay: View
	private lateinit var pairSection: View
	private lateinit var footer: View
	private val store by lazy { PairingStore(this) }
	private val prefs by lazy { getPreferences(MODE_PRIVATE) }
	private var startWhenAllowed = false
	private val observer: (String) -> Unit = { render() }

	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_main)
		content = findViewById(R.id.content)
		badge = findViewById(R.id.status_badge)
		icon = findViewById(R.id.status_icon)
		title = findViewById(R.id.title)
		detail = findViewById(R.id.detail)
		primary = findViewById(R.id.primary)
		accessHeader = findViewById(R.id.access_header)
		accessSection = findViewById(R.id.access_section)
		accessPhone = findViewById(R.id.access_phone)
		accessDivider = findViewById(R.id.access_divider)
		accessOverlay = findViewById(R.id.access_overlay)
		pairSection = findViewById(R.id.pair_section)
		footer = findViewById(R.id.footer)
		val top = content.paddingTop
		val bottom = content.paddingBottom
		val side = content.paddingLeft
		content.setOnApplyWindowInsetsListener { view, insets ->
			val bars = insets.getInsets(WindowInsets.Type.systemBars() or WindowInsets.Type.displayCutout())
			view.setPadding(side + bars.left, top + bars.top, side + bars.right, bottom + bars.bottom + (24 * resources.displayMetrics.density).toInt())
			insets
		}
		primary.setOnClickListener {
			when {
				store.load() == null -> scan()
				BridgeState.running -> stopBridge()
				else -> startBridge()
			}
		}
		findViewById<View>(R.id.pair_other).setOnClickListener { scan() }
		accessPhone.setOnClickListener { requestAccess() }
		accessOverlay.setOnClickListener {
			startActivity(Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName")))
		}
		acceptDeepLink(intent)
	}

	override fun onNewIntent(intent: Intent) {
		super.onNewIntent(intent)
		acceptDeepLink(intent)
		render()
	}

	override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
		super.onRequestPermissionsResult(requestCode, permissions, grantResults)
		if (BridgeState.running) startService(Intent(this, BridgeService::class.java).setAction(BridgeService.ACTION_CONTACTS))
		startIfAllowed()
		render()
	}

	override fun onStart() {
		super.onStart()
		BridgeState.observe(observer)
	}

	override fun onResume() {
		super.onResume()
		startIfAllowed()
		render()
	}

	override fun onStop() {
		BridgeState.unobserve(observer)
		super.onStop()
	}

	@Deprecated("Deprecated in Java")
	override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
		super.onActivityResult(requestCode, resultCode, data)
		if (requestCode == REQUEST_SCAN && resultCode == RESULT_OK) startBridge()
	}

	private fun render() {
		val pairing = store.load()
		val running = BridgeState.running
		TransitionManager.beginDelayedTransition(content)
		val (tint, glyph, heading) = when {
			pairing == null -> Triple(R.color.accent, R.drawable.ic_qr, R.string.title_unpaired)
			BridgeState.connected -> Triple(R.color.green, R.drawable.ic_check, R.string.title_connected)
			running -> Triple(R.color.orange, R.drawable.ic_sync, R.string.title_connecting)
			else -> Triple(R.color.gray, R.drawable.ic_close, R.string.title_disconnected)
		}
		badge.backgroundTintList = getColorStateList(tint)
		icon.setImageResource(glyph)
		title.setText(heading)
		detail.text = if (pairing == null) getString(R.string.hint_unpaired) else BridgeState.status.ifEmpty { pairing.name }
		primary.setText(
			when {
				pairing == null -> R.string.scan_qr
				running -> R.string.disconnect
				else -> R.string.connect
			}
		)
		primary.setBackgroundResource(if (pairing != null && running) R.drawable.btn_secondary else R.drawable.btn_primary)
		primary.setTextColor(getColor(if (pairing != null && running) R.color.red_text else R.color.on_fill))
		val needsPhone = pairing != null && missingPermissions().isNotEmpty()
		val needsOverlay = pairing != null && !Settings.canDrawOverlays(this)
		accessPhone.visibility = visibleIf(needsPhone)
		accessOverlay.visibility = visibleIf(needsOverlay)
		accessDivider.visibility = visibleIf(needsPhone && needsOverlay)
		accessHeader.visibility = visibleIf(needsPhone || needsOverlay)
		accessSection.visibility = visibleIf(needsPhone || needsOverlay)
		pairSection.visibility = visibleIf(pairing != null)
		footer.visibility = visibleIf(pairing != null)
	}

	private fun visibleIf(condition: Boolean) = if (condition) View.VISIBLE else View.GONE

	private fun missingPermissions() = PERMISSIONS.filter { checkSelfPermission(it) != PackageManager.PERMISSION_GRANTED }

	private fun requestAccess() {
		val missing = missingPermissions()
		if (missing.isEmpty()) return
		if (prefs.getBoolean(KEY_ASKED, false) && missing.none(::shouldShowRequestPermissionRationale)) {
			startActivity(Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.parse("package:$packageName")))
			return
		}
		prefs.edit().putBoolean(KEY_ASKED, true).apply()
		requestPermissions(missing.toTypedArray(), REQUEST_ACCESS)
	}

	private fun scan() {
		startActivityForResult(Intent(this, ScanActivity::class.java), REQUEST_SCAN)
	}

	private fun acceptDeepLink(intent: Intent) {
		val pairing = intent.dataString?.let(Pairing::parse) ?: return
		store.save(pairing)
		intent.data = null
		startBridge()
	}

	private fun microphoneAllowed() = checkSelfPermission(Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED

	private fun startBridge() {
		if (store.load() == null) return
		if (microphoneAllowed() && (missingPermissions().isEmpty() || prefs.getBoolean(KEY_ASKED, false))) {
			launchService()
		} else {
			startWhenAllowed = true
			requestAccess()
		}
	}

	private fun startIfAllowed() {
		if (startWhenAllowed && microphoneAllowed()) {
			startWhenAllowed = false
			launchService()
		}
	}

	private fun launchService() {
		startForegroundService(Intent(this, BridgeService::class.java))
	}

	private fun stopBridge() {
		startService(Intent(this, BridgeService::class.java).setAction(BridgeService.ACTION_STOP))
	}
}
