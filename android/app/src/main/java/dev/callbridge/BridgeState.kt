package dev.callbridge

import android.os.Handler
import android.os.Looper

/** Connection status shown by the activity. */
object BridgeState {
	private val handler = Handler(Looper.getMainLooper())
	private val listeners = mutableSetOf<(String) -> Unit>()

	var status: String = ""
		private set

	var connected: Boolean = false
		private set

	var running: Boolean = false

	fun update(text: String, connected: Boolean = false) {
		status = text
		this.connected = connected
		handler.post { listeners.toList().forEach { it(text) } }
	}

	fun observe(listener: (String) -> Unit) {
		listeners.add(listener)
		listener(status)
	}

	fun unobserve(listener: (String) -> Unit) {
		listeners.remove(listener)
	}
}
