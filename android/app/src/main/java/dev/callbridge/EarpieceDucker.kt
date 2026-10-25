package dev.callbridge

import android.content.Context
import android.media.AudioManager

private const val SAVED_VOLUME = "saved_voice_volume"

/** Turns the phone's own call volume down while the call is heard on the PC, and puts it back afterwards. */
class EarpieceDucker(context: Context) {
	private val audio = context.getSystemService(AudioManager::class.java)
	private val prefs = context.getSharedPreferences("bridge", Context.MODE_PRIVATE)

	fun duck() {
		if (prefs.contains(SAVED_VOLUME)) return
		prefs.edit().putInt(SAVED_VOLUME, audio.getStreamVolume(AudioManager.STREAM_VOICE_CALL)).apply()
		audio.setStreamVolume(AudioManager.STREAM_VOICE_CALL, audio.getStreamMinVolume(AudioManager.STREAM_VOICE_CALL), 0)
	}

	/** Also covers a crash while ducked: the saved value survives in preferences. */
	fun restore() {
		if (!prefs.contains(SAVED_VOLUME)) return
		audio.setStreamVolume(AudioManager.STREAM_VOICE_CALL, prefs.getInt(SAVED_VOLUME, 1), 0)
		prefs.edit().remove(SAVED_VOLUME).apply()
	}
}
