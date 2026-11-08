package dev.callbridge

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.telecom.Call
import android.telecom.InCallService

private const val EVENT_HOLD_FAILED = "android.telecom.event.CALL_HOLD_FAILED"
private const val DTMF_TONE_MS = 250L

class BridgeInCallService : InCallService() {
	override fun onCallAdded(call: Call) {
		CallRelay.add(call)
	}

	override fun onCallRemoved(call: Call) {
		CallRelay.remove(call)
	}
}

object CallRelay {
	private val main = Handler(Looper.getMainLooper())
	private val calls = ArrayList<Call>()
	var onHoldChanged: ((Boolean) -> Unit)? = null
	var onHoldFailed: (() -> Unit)? = null

	private val callback = object : Call.Callback() {
		override fun onStateChanged(call: Call, state: Int) {
			notifyHold()
		}

		override fun onConnectionEvent(call: Call, event: String?, extras: Bundle?) {
			if (event == EVENT_HOLD_FAILED) {
				Log.i("CallBridge", "hold refused by the network")
				onHoldFailed?.invoke()
			}
		}
	}

	fun add(call: Call) {
		main.post {
			calls.add(call)
			call.registerCallback(callback)
			notifyHold()
		}
	}

	fun remove(call: Call) {
		main.post {
			call.unregisterCallback(callback)
			calls.remove(call)
			notifyHold()
		}
	}

	val connected: Boolean get() = current() != null

	val holding: Boolean get() = current()?.state == Call.STATE_HOLDING

	fun hold(on: Boolean): Boolean {
		val call = current()
		Log.i("CallBridge", "hold on=$on calls=${calls.size} state=${call?.state} canHold=${call?.details?.can(Call.Details.CAPABILITY_HOLD)}")
		if (call == null) return false
		if (on) {
			if (call.state != Call.STATE_ACTIVE || !call.details.can(Call.Details.CAPABILITY_HOLD)) return false
			call.hold()
		} else {
			if (call.state != Call.STATE_HOLDING) return false
			call.unhold()
		}
		return true
	}

	fun dtmf(digit: Char): Boolean {
		val call = current()
		Log.i("CallBridge", "dtmf digit=$digit calls=${calls.size} state=${call?.state}")
		if (call == null || call.state != Call.STATE_ACTIVE) return false
		call.playDtmfTone(digit)
		main.postDelayed({ call.stopDtmfTone() }, DTMF_TONE_MS)
		return true
	}

	private fun current(): Call? = calls.lastOrNull { it.state != Call.STATE_DISCONNECTED && it.state != Call.STATE_DISCONNECTING }

	private fun notifyHold() {
		onHoldChanged?.invoke(holding)
	}
}
