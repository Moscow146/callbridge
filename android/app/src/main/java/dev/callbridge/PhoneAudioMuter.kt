package dev.callbridge

import android.content.Context
import android.media.AudioManager

class PhoneAudioMuter(private val context: Context) {
	private val audio = context.getSystemService(AudioManager::class.java)
	private val reapply = AudioManager.OnCommunicationDeviceChangedListener { apply(true) }
	private var muted = false
	private var savedVolume = -1

	fun duck() {
		if (savedVolume >= 0) return
		savedVolume = audio.getStreamVolume(AudioManager.STREAM_VOICE_CALL)
		audio.setStreamVolume(AudioManager.STREAM_VOICE_CALL, minOf(1, savedVolume), 0)
	}

	private fun unduck() {
		if (savedVolume < 0) return
		audio.setStreamVolume(AudioManager.STREAM_VOICE_CALL, savedVolume, 0)
		savedVolume = -1
	}

	fun mute() {
		unduck()
		apply(true)
		if (muted) return
		muted = true
		audio.addOnCommunicationDeviceChangedListener(context.mainExecutor, reapply)
	}

	fun restore() {
		unduck()
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
