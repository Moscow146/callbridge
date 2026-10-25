package dev.callbridge

import java.io.IOException
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.SocketTimeoutException

private const val DISCOVERY_PORT = 47801
private const val QUERY = "CALLBRIDGE?"
private const val LISTEN_MS = 1200

/** Finds PCs on the local network that answer the broadcast, the one named [name] first. */
object Discovery {
	fun find(name: String): List<String> {
		val found = LinkedHashMap<String, String>()
		try {
			DatagramSocket().use { socket ->
				socket.broadcast = true
				val query = QUERY.toByteArray()
				socket.send(DatagramPacket(query, query.size, InetAddress.getByName("255.255.255.255"), DISCOVERY_PORT))
				val deadline = System.currentTimeMillis() + LISTEN_MS
				val buffer = ByteArray(256)
				while (true) {
					val left = (deadline - System.currentTimeMillis()).toInt()
					if (left <= 0) break
					socket.soTimeout = left
					val packet = DatagramPacket(buffer, buffer.size)
					try {
						socket.receive(packet)
					} catch (e: SocketTimeoutException) {
						break
					}
					val parts = String(packet.data, 0, packet.length).split(' ', limit = 3)
					if (parts.size == 3 && parts[0] == "CALLBRIDGE") found[packet.address.hostAddress.orEmpty()] = parts[2]
				}
			}
		} catch (e: IOException) {
			return emptyList()
		}
		return found.entries.sortedByDescending { it.value == name }.map { it.key }
	}
}
