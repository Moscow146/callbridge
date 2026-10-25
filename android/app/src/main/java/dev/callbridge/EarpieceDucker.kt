package dev.callbridge

import android.content.Context
import android.media.AudioManager

private const val MUTED = "voice_call_muted"

/** Silences the phone's own call output while the call is heard on the PC, and unmutes it afterwards. */
class EarpieceDucker(context: Context) {
	private val audio = context.getSystemService(AudioManager::class.java)
	private val prefs = context.getSharedPreferences("bridge", Context.MODE_PRIVATE)

	fun duck() {
		if (prefs.getBoolean(MUTED, false)) return
		prefs.edit().putBoolean(MUTED, true).apply()
		audio.adjustStreamVolume(AudioManager.STREAM_VOICE_CALL, AudioManager.ADJUST_MUTE, 0)
	}

	/** Also covers a crash while muted: the flag survives in preferences. */
	fun restore() {
		if (!prefs.getBoolean(MUTED, false)) return
		audio.adjustStreamVolume(AudioManager.STREAM_VOICE_CALL, AudioManager.ADJUST_UNMUTE, 0)
		prefs.edit().putBoolean(MUTED, false).apply()
	}
}
