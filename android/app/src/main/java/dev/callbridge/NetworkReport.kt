package dev.callbridge

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import android.util.Log

private const val TAG = "CallBridge"

object NetworkReport {
	fun label(wifi: Boolean, cellular: Boolean, vpn: Boolean, ethernet: Boolean): String {
		val parts = buildList {
			if (wifi) add("wifi")
			if (cellular) add("cellular")
			if (ethernet) add("ethernet")
			if (vpn) add("vpn")
		}
		return if (parts.isEmpty()) "unknown" else parts.joinToString("+")
	}

	fun describe(capabilities: NetworkCapabilities?): String = if (capabilities == null) "gone" else label(
		capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI),
		capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR),
		capabilities.hasTransport(NetworkCapabilities.TRANSPORT_VPN),
		capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET),
	)

	fun snapshot(context: Context, bound: Network?): String {
		val manager = context.getSystemService(ConnectivityManager::class.java)
		val active = manager.activeNetwork
		val all = manager.allNetworks.joinToString(", ") { "${it}=${describe(manager.getNetworkCapabilities(it))}" }
		return "active=${describe(manager.getNetworkCapabilities(active))}, bound=${bound ?: "none"}, networks=[$all]"
	}

	fun watch(context: Context): ConnectivityManager.NetworkCallback {
		val manager = context.getSystemService(ConnectivityManager::class.java)
		val callback = object : ConnectivityManager.NetworkCallback() {
			override fun onAvailable(network: Network) {
				Log.i(TAG, "network available: $network")
			}

			override fun onCapabilitiesChanged(network: Network, capabilities: NetworkCapabilities) {
				Log.i(TAG, "network $network is ${describe(capabilities)}")
			}

			override fun onLost(network: Network) {
				Log.i(TAG, "network lost: $network")
			}
		}
		manager.registerNetworkCallback(NetworkRequest.Builder().build(), callback)
		return callback
	}

	fun unwatch(context: Context, callback: ConnectivityManager.NetworkCallback) {
		runCatching { context.getSystemService(ConnectivityManager::class.java).unregisterNetworkCallback(callback) }
	}
}
