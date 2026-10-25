package dev.callbridge

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
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

class MainActivity : Activity() {
	private lateinit var host: EditText
	private lateinit var code: EditText
	private lateinit var toggle: Button
	private lateinit var status: TextView
	private val prefs by lazy { getSharedPreferences("bridge", Context.MODE_PRIVATE) }
	private val observer: (String) -> Unit = { text ->
		status.text = text
		toggle.setText(if (BridgeState.running) R.string.disconnect else R.string.connect)
	}

	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_main)
		host = findViewById(R.id.host)
		code = findViewById(R.id.code)
		toggle = findViewById(R.id.toggle)
		status = findViewById(R.id.status)
		host.setText(prefs.getString("host", ""))
		toggle.setOnClickListener { if (BridgeState.running) stopBridge() else startBridge() }
		val missing = PERMISSIONS.filter { checkSelfPermission(it) != PackageManager.PERMISSION_GRANTED }
		if (missing.isNotEmpty()) requestPermissions(missing.toTypedArray(), 1)
	}

	override fun onStart() {
		super.onStart()
		BridgeState.observe(observer)
	}

	override fun onStop() {
		BridgeState.unobserve(observer)
		super.onStop()
	}

	private fun startBridge() {
		val address = host.text.toString().trim()
		val pairing = code.text.toString().trim()
		if (address.isEmpty() || pairing.length != 6) {
			status.text = "Enter the PC address and the 6-digit code"
			return
		}
		prefs.edit().putString("host", address).apply()
		startForegroundService(
			Intent(this, BridgeService::class.java)
				.putExtra(BridgeService.EXTRA_HOST, address)
				.putExtra(BridgeService.EXTRA_CODE, pairing)
		)
		toggle.setText(R.string.disconnect)
	}

	private fun stopBridge() {
		startService(Intent(this, BridgeService::class.java).setAction(BridgeService.ACTION_STOP))
		toggle.setText(R.string.connect)
	}
}
