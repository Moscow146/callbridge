package dev.callbridge

import android.content.Context
import android.net.Uri

/** A paired PC, decoded from callbridge://pair?h=a,b&p=47800&c=token&n=name. */
data class Pairing(val hosts: List<String>, val port: Int, val code: String, val name: String) {
	companion object {
		fun parse(text: String): Pairing? {
			val uri = Uri.parse(text)
			if (uri.scheme != "callbridge" || uri.host != "pair") return null
			val hosts = uri.getQueryParameter("h")?.split(',')?.filter { it.isNotBlank() }.orEmpty()
			val code = uri.getQueryParameter("c")
			if (hosts.isEmpty() || code.isNullOrEmpty()) return null
			val port = uri.getQueryParameter("p")?.toIntOrNull() ?: DEFAULT_PORT
			return Pairing(hosts, port, code, uri.getQueryParameter("n") ?: hosts.first())
		}
	}
}

class PairingStore(context: Context) {
	private val prefs = context.getSharedPreferences("bridge", Context.MODE_PRIVATE)

	fun save(pairing: Pairing) {
		prefs.edit()
			.putString("hosts", pairing.hosts.joinToString(","))
			.putInt("port", pairing.port)
			.putString("code", pairing.code)
			.putString("name", pairing.name)
			.apply()
	}

	fun load(): Pairing? {
		val hosts = prefs.getString("hosts", null)?.split(',')?.filter { it.isNotBlank() }.orEmpty()
		val code = prefs.getString("code", null)
		if (hosts.isEmpty() || code == null) return null
		return Pairing(hosts, prefs.getInt("port", DEFAULT_PORT), code, prefs.getString("name", null) ?: hosts.first())
	}
}
