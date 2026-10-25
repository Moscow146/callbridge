package dev.callbridge

import android.annotation.SuppressLint
import android.content.Context
import android.net.Uri
import android.os.Bundle
import android.provider.ContactsContract
import android.provider.ContactsContract.CommonDataKinds.Phone
import android.telecom.TelecomManager
import org.json.JSONArray
import org.json.JSONObject

private const val MAX_CONTACTS_JSON = 800_000

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

	/** Every contact phone number as {name, number}, capped so the control frame stays small. */
	fun contacts(): JSONArray {
		val result = JSONArray()
		val seen = HashSet<String>()
		var size = 0
		runCatching {
			context.contentResolver.query(
				Phone.CONTENT_URI, arrayOf(Phone.DISPLAY_NAME, Phone.NUMBER), null, null, "${Phone.DISPLAY_NAME} COLLATE LOCALIZED ASC"
			)?.use { cursor ->
				while (cursor.moveToNext()) {
					val name = cursor.getString(0) ?: continue
					val number = cursor.getString(1) ?: continue
					if (!seen.add(name + number.filter(Char::isDigit))) continue
					val item = JSONObject().put("name", name).put("number", number)
					size += item.toString().length
					if (size > MAX_CONTACTS_JSON) break
					result.put(item)
				}
			}
		}
		return result
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
