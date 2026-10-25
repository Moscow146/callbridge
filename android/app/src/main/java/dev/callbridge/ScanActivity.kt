package dev.callbridge

import android.Manifest
import android.content.pm.PackageManager
import android.os.Bundle
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
	private lateinit var preview: PreviewView

	override fun onCreate(savedInstanceState: Bundle?) {
		super.onCreate(savedInstanceState)
		preview = PreviewView(this)
		setContentView(preview)
		if (checkSelfPermission(Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED) {
			startCamera()
		} else {
			requestPermissions(arrayOf(Manifest.permission.CAMERA), 1)
		}
	}

	override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<String>, grantResults: IntArray) {
		super.onRequestPermissionsResult(requestCode, permissions, grantResults)
		if (grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED) startCamera() else finish()
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
