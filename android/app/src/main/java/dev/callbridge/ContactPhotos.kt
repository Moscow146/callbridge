package dev.callbridge

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.net.Uri
import android.provider.ContactsContract
import android.util.Base64
import java.io.ByteArrayOutputStream

const val PHOTO_SIZE = 96
const val PHOTO_MAX_BYTES = 8 * 1024

object Thumbnail {
	fun cropSquare(width: Int, height: Int): IntArray {
		val side = minOf(width, height)
		return intArrayOf((width - side) / 2, (height - side) / 2, side, side)
	}

	fun sampleSize(width: Int, height: Int, target: Int): Int {
		var sample = 1
		val side = minOf(width, height)
		while (side / (sample * 2) >= target) sample *= 2
		return sample
	}

	fun qualities(): IntArray = intArrayOf(80, 70, 60, 50, 40)
}

class ContactPhotos(private val context: Context) {
	fun photoBase64(number: String): String? {
		val bytes = thumbnail(number) ?: return null
		return Base64.encodeToString(bytes, Base64.NO_WRAP)
	}

	private fun thumbnail(number: String): ByteArray? {
		if (number.isEmpty()) return null
		val lookup = Uri.withAppendedPath(ContactsContract.PhoneLookup.CONTENT_FILTER_URI, Uri.encode(number))
		return runCatching {
			val photoUri = context.contentResolver.query(lookup, arrayOf(ContactsContract.PhoneLookup.PHOTO_THUMBNAIL_URI), null, null, null)?.use {
				if (it.moveToFirst()) it.getString(0) else null
			} ?: return null
			val source = decode(Uri.parse(photoUri)) ?: return null
			encode(source)
		}.getOrNull()
	}

	private fun decode(uri: Uri): Bitmap? {
		val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
		context.contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it, null, bounds) }
		if (bounds.outWidth <= 0 || bounds.outHeight <= 0) return null
		val options = BitmapFactory.Options().apply { inSampleSize = Thumbnail.sampleSize(bounds.outWidth, bounds.outHeight, PHOTO_SIZE) }
		return context.contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it, null, options) }
	}

	private fun encode(source: Bitmap): ByteArray? {
		val crop = Thumbnail.cropSquare(source.width, source.height)
		val square = Bitmap.createBitmap(source, crop[0], crop[1], crop[2], crop[3])
		val scaled = Bitmap.createScaledBitmap(square, PHOTO_SIZE, PHOTO_SIZE, true)
		for (quality in Thumbnail.qualities()) {
			val out = ByteArrayOutputStream()
			scaled.compress(Bitmap.CompressFormat.JPEG, quality, out)
			if (out.size() <= PHOTO_MAX_BYTES) return out.toByteArray()
		}
		return null
	}
}
