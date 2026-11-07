package dev.callbridge

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Bundle
import android.provider.Settings
import android.view.View
import android.view.WindowInsets
import android.widget.TextView
import androidx.activity.ComponentActivity
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.core.content.ContextCompat
import com.google.zxing.BarcodeFormat
import com.google.zxing.BinaryBitmap
import com.google.zxing.DecodeHintType
import com.google.zxing.MultiFormatReader
import com.google.zxing.NotFoundException
import com.google.zxing.PlanarYUVLuminanceSource
import com.google.zxing.common.HybridBinarizer
import java.util.concurrent.Executors

/** Scans the pairing QR shown by the PC app. */
class ScanActivity : ComponentActivity() {
	private val executor = Executors.newSingleThreadExecutor()
	private val reader = MultiFormatReader().apply {
		setHints(mapOf(DecodeHintType.POSSIBLE_FORMATS to listOf(BarcodeFormat.QR_CODE)))
	}
	@Volatile private var done = false
	private var cameraStarted = false
	private lateinit var preview: PreviewView
	private lateinit var hint: TextView
	private lateinit var openSettings: View

	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_scan)
		preview = findViewById(R.id.preview)
		hint = findViewById(R.id.hint)
		openSettings = findViewById(R.id.open_settings)
		findViewById<View>(R.id.cancel).setOnClickListener { finish() }
		openSettings.setOnClickListener {
			startActivity(Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS, Uri.parse("package:$packageName")))
		}
		findViewById<View>(R.id.root).setOnApplyWindowInsetsListener { view, insets ->
			val bars = insets.getInsets(WindowInsets.Type.systemBars() or WindowInsets.Type.displayCutout())
			view.setPadding(bars.left, bars.top, bars.right, bars.bottom)
			insets
		}
		if (cameraAllowed()) showCamera() else requestPermissions(arrayOf(Manifest.permission.CAMERA), 1)
	}

	override fun onResume() {
		super.onResume()
		if (cameraAllowed()) showCamera()
	}

	override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<String>, grantResults: IntArray) {
		super.onRequestPermissionsResult(requestCode, permissions, grantResults)
		if (grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED) {
			showCamera()
		} else {
			hint.setText(R.string.scan_no_camera)
			openSettings.visibility = View.VISIBLE
		}
	}

	private fun cameraAllowed() = checkSelfPermission(Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED

	private fun showCamera() {
		hint.setText(R.string.scan_hint)
		openSettings.visibility = View.GONE
		if (cameraStarted) return
		cameraStarted = true
		startCamera()
	}

	private fun startCamera() {
		val future = ProcessCameraProvider.getInstance(this)
		future.addListener({
			val provider = future.get()
			val previewUseCase = Preview.Builder().build().also { it.surfaceProvider = preview.surfaceProvider }
			val analysis = ImageAnalysis.Builder()
				.setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
				.build()
			analysis.setAnalyzer(executor, ::analyze)
			provider.unbindAll()
			provider.bindToLifecycle(this, CameraSelector.DEFAULT_BACK_CAMERA, previewUseCase, analysis)
		}, ContextCompat.getMainExecutor(this))
	}

	private fun analyze(image: ImageProxy) {
		try {
			if (done) return
			val plane = image.planes[0]
			val buffer = plane.buffer
			val data = ByteArray(buffer.remaining())
			buffer.get(data)
			val source = PlanarYUVLuminanceSource(data, plane.rowStride, image.height, 0, 0, image.width, image.height, false)
			val text = try {
				reader.decodeWithState(BinaryBitmap(HybridBinarizer(source))).text
			} catch (e: NotFoundException) {
				return
			} finally {
				reader.reset()
			}
			val pairing = Pairing.parse(text) ?: return
			done = true
			runOnUiThread {
				PairingStore(this).save(pairing)
				setResult(RESULT_OK)
				finish()
			}
		} finally {
			image.close()
		}
	}

	override fun onDestroy() {
		executor.shutdown()
		super.onDestroy()
	}
}
