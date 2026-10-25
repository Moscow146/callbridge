package dev.callbridge

import android.os.Handler
import android.os.Looper

/** Connection status shown by the activity. */
object BridgeState {
	private val handler = Handler(Looper.getMainLooper())
	private val listeners = mutableSetOf<(String) -> Unit>()

	var status: String = "Disconnected"
		private set

	var running: Boolean = false

	fun update(text: String) {
		status = text
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
