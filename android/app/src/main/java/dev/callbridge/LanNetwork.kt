package dev.callbridge

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import java.net.Inet4Address
import java.net.InetAddress

/** The phone's Wi-Fi network, so LAN traffic to the PC does not end up inside an active VPN. */
object LanNetwork {
	@Suppress("DEPRECATION")
	fun wifi(context: Context): Network? {
		val manager = context.getSystemService(ConnectivityManager::class.java)
		return manager.allNetworks.firstOrNull { network ->
			val capabilities = manager.getNetworkCapabilities(network)
			capabilities != null &&
				capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) &&
				!capabilities.hasTransport(NetworkCapabilities.TRANSPORT_VPN)
		}
	}

	/** Broadcast address of the network's IPv4 subnet, e.g. 192.168.0.255. */
	fun broadcast(context: Context, network: Network?): InetAddress? {
		network ?: return null
		val manager = context.getSystemService(ConnectivityManager::class.java)
		val link = manager.getLinkProperties(network)?.linkAddresses?.firstOrNull { it.address is Inet4Address } ?: return null
		val address = link.address.address
		val mask = if (link.prefixLength == 0) 0 else -1 shl (32 - link.prefixLength)
		val ip = address.fold(0) { acc, byte -> (acc shl 8) or (byte.toInt() and 0xFF) }
		val result = ip or mask.inv()
		return InetAddress.getByAddress(ByteArray(4) { index -> (result ushr (24 - 8 * index)).toByte() })
	}
}
