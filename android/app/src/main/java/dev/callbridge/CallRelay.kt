package dev.callbridge

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.telecom.Call
import android.telecom.InCallService
import android.util.Log

private const val EVENT_HOLD_FAILED = "android.telecom.event.CALL_HOLD_FAILED"
private const val DTMF_TONE_MS = 250L

class BridgeInCallService : InCallService() {
	override fun onCreate() {
		super.onCreate()
		StartReceiver.ensureRunning(this, "the in-call service started")
	}

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
	var onChanged: (() -> Unit)? = null
	var onHoldFailed: (() -> Unit)? = null

	private val callback = object : Call.Callback() {
		override fun onStateChanged(call: Call, state: Int) {
			notifyChanged()
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
			notifyChanged()
		}
	}

	fun remove(call: Call) {
		main.post {
			call.unregisterCallback(callback)
			calls.remove(call)
			notifyChanged()
		}
	}

	val connected: Boolean get() = current() != null

	val number: String? get() = current()?.details?.handle?.schemeSpecificPart?.takeIf { it.isNotEmpty() }

	val phase: String? get() = when (current()?.state) {
		Call.STATE_NEW, Call.STATE_CONNECTING, Call.STATE_SELECT_PHONE_ACCOUNT -> "connecting"
		Call.STATE_DIALING, Call.STATE_PULLING_CALL -> "dialing"
		Call.STATE_RINGING -> "ringing"
		Call.STATE_ACTIVE -> "active"
		Call.STATE_HOLDING -> "held"
		else -> null
	}

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

	private fun notifyChanged() {
		onChanged?.invoke()
	}
}
