package dev.callbridge

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log

class StartReceiver : BroadcastReceiver() {
	override fun onReceive(context: Context, intent: Intent) {
		ensureRunning(context, intent.action.orEmpty())
	}

	companion object {
		fun ensureRunning(context: Context, reason: String) {
			val store = PairingStore(context)
			if (store.load() == null || !store.autostart || BridgeState.running) return
			Log.i("CallBridge", "starting the service after $reason")
			runCatching { context.startForegroundService(Intent(context, BridgeService::class.java)) }
				.onFailure { Log.w("CallBridge", "service start refused: ${it.message}") }
		}
	}
}
