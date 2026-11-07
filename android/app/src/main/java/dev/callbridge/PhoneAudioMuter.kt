package dev.callbridge

import android.content.Context
import android.media.AudioManager

class PhoneAudioMuter(private val context: Context) {
	private val audio = context.getSystemService(AudioManager::class.java)
	private val reapply = AudioManager.OnCommunicationDeviceChangedListener { apply(true) }
	private var muted = false

	fun mute() {
		apply(true)
		if (muted) return
		muted = true
		audio.addOnCommunicationDeviceChangedListener(context.mainExecutor, reapply)
	}

	fun restore() {
		if (muted) {
			muted = false
			audio.removeOnCommunicationDeviceChangedListener(reapply)
		}
		apply(false)
	}

	private fun apply(mute: Boolean) {
		audio.setParameters("device_mute=$mute;direction=rx")
		audio.setParameters("device_mute=$mute;direction=tx")
	}
}
