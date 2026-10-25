package dev.callbridge

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.animation.ObjectAnimator
import android.animation.ValueAnimator
import android.graphics.RenderEffect
import android.graphics.Shader
import android.os.Bundle
import android.view.View
import android.view.WindowInsets
import android.view.animation.AccelerateDecelerateInterpolator
import android.widget.Button
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

private const val REQUEST_SCAN = 2

class MainActivity : Activity() {
	private lateinit var pcName: TextView
	private lateinit var toggle: Button
	private lateinit var scan: Button
	private lateinit var status: TextView
	private val store by lazy { PairingStore(this) }
	private lateinit var pill: TextView
	private val observer: (String) -> Unit = { text ->
		status.text = text.ifEmpty { getString(R.string.status_disconnected) }
		renderToggle()
		pill.setText(
			when {
				BridgeState.connected -> R.string.pill_connected
				BridgeState.running -> R.string.pill_connecting
				else -> R.string.pill_offline
			}
		)
	}

	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_main)
		pcName = findViewById(R.id.pc_name)
		toggle = findViewById(R.id.toggle)
		scan = findViewById(R.id.scan)
		status = findViewById(R.id.status)
		pill = findViewById(R.id.pill)
		setupGlass()
		toggle.setOnClickListener { if (BridgeState.running) stopBridge() else startBridge() }
		scan.setOnClickListener { startActivityForResult(Intent(this, ScanActivity::class.java), REQUEST_SCAN) }
		val missing = PERMISSIONS.filter { checkSelfPermission(it) != PackageManager.PERMISSION_GRANTED }
		if (missing.isNotEmpty()) requestPermissions(missing.toTypedArray(), 1)
		acceptDeepLink(intent)
		refresh()
	}

	override fun onNewIntent(intent: Intent) {
		super.onNewIntent(intent)
		acceptDeepLink(intent)
		refresh()
	}

	override fun onStart() {
		super.onStart()
		BridgeState.observe(observer)
	}

	override fun onStop() {
		BridgeState.unobserve(observer)
		super.onStop()
	}

	@Deprecated("Deprecated in Java")
	override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
		super.onActivityResult(requestCode, resultCode, data)
		if (requestCode == REQUEST_SCAN && resultCode == RESULT_OK) {
			refresh()
			startBridge()
		}
	}

	private fun setupGlass() {
		val density = resources.displayMetrics.density
		val content = findViewById<View>(R.id.content)
		val basePadding = content.paddingLeft
		content.setOnApplyWindowInsetsListener { view, insets ->
			val bars = insets.getInsets(WindowInsets.Type.systemBars())
			view.setPadding(basePadding + bars.left, basePadding + bars.top, basePadding + bars.right, basePadding + bars.bottom)
			insets
		}
		findViewById<View>(R.id.orbs).setRenderEffect(RenderEffect.createBlurEffect(90 * density, 90 * density, Shader.TileMode.CLAMP))
		drift(R.id.orb_blue, 90f * density, 120f * density, 14000)
		drift(R.id.orb_pink, -100f * density, -110f * density, 18000)
		drift(R.id.orb_teal, 80f * density, -90f * density, 21000)
	}

	private fun drift(id: Int, dx: Float, dy: Float, duration: Long) {
		val orb = findViewById<View>(id)
		for ((property, distance) in listOf("translationX" to dx, "translationY" to dy)) {
			ObjectAnimator.ofFloat(orb, property, 0f, distance).apply {
				this.duration = duration
				repeatCount = ValueAnimator.INFINITE
				repeatMode = ValueAnimator.REVERSE
				interpolator = AccelerateDecelerateInterpolator()
			}.start()
		}
	}

	private fun renderToggle() {
		toggle.setText(if (BridgeState.running) R.string.disconnect else R.string.connect)
		toggle.setBackgroundResource(if (BridgeState.running) R.drawable.btn_red else R.drawable.btn_green)
	}

	private fun acceptDeepLink(intent: Intent) {
		val pairing = intent.dataString?.let(Pairing::parse) ?: return
		store.save(pairing)
		intent.data = null
		startBridge()
	}

	private fun refresh() {
		val pairing = store.load()
		pcName.text = pairing?.let { getString(R.string.paired_with, it.name) } ?: getString(R.string.not_paired)
		toggle.visibility = if (pairing == null) View.GONE else View.VISIBLE
		scan.setText(if (pairing == null) R.string.scan_qr else R.string.pair_other)
	}

	private fun startBridge() {
		if (store.load() == null) return
		startForegroundService(Intent(this, BridgeService::class.java))
		toggle.setText(R.string.disconnect)
	}

	private fun stopBridge() {
		startService(Intent(this, BridgeService::class.java).setAction(BridgeService.ACTION_STOP))
		toggle.setText(R.string.connect)
	}
}
