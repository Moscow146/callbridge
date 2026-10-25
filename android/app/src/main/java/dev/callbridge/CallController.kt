package dev.callbridge

import android.annotation.SuppressLint
import android.content.Context
import android.net.Uri
import android.os.Bundle
import android.provider.ContactsContract
import android.telecom.TelecomManager

/** Answers, ends and places calls through the system Telecom service. */
class CallController(private val context: Context) {
	private val telecom = context.getSystemService(TelecomManager::class.java)

	@SuppressLint("MissingPermission")
	fun answer(): Boolean {
		telecom.acceptRingingCall()
		return true
	}

	@SuppressLint("MissingPermission")
	fun hangup(): Boolean = telecom.endCall()

	@SuppressLint("MissingPermission")
	fun dial(number: String) {
		telecom.placeCall(Uri.fromParts("tel", number, null), Bundle())
	}

	fun contactName(number: String?): String? {
		if (number.isNullOrEmpty()) return null
		val uri = Uri.withAppendedPath(ContactsContract.PhoneLookup.CONTENT_FILTER_URI, Uri.encode(number))
		return runCatching {
			context.contentResolver.query(uri, arrayOf(ContactsContract.PhoneLookup.DISPLAY_NAME), null, null, null)?.use {
				if (it.moveToFirst()) it.getString(0) else null
			}
		}.getOrNull()
	}
}
