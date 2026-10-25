package dev.callbridge

import android.net.Network
import android.util.Log
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
	fun find(name: String, lan: Network?, broadcast: InetAddress?): List<String> {
		val found = LinkedHashMap<String, String>()
		try {
			DatagramSocket().use { socket ->
				lan?.bindSocket(socket)
				socket.broadcast = true
				val query = QUERY.toByteArray()
				for (target in listOfNotNull(broadcast, InetAddress.getByName("255.255.255.255"))) {
					try {
						socket.send(DatagramPacket(query, query.size, target, DISCOVERY_PORT))
					} catch (e: IOException) {
						Log.w("CallBridge", "discovery send to $target failed: ${e.message}")
					}
				}
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
			Log.w("CallBridge", "discovery failed: ${e.message}")
			return emptyList()
		}
		return found.entries.sortedByDescending { it.value == name }.map { it.key }
	}
}
