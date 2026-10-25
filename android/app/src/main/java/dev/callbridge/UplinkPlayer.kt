package dev.callbridge

import android.content.Context
import android.media.AudioAttributes
import android.media.AudioDeviceInfo
import android.media.AudioFormat
import android.media.AudioManager
import android.media.AudioTrack
import android.util.Log
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.TimeUnit

private const val QUEUE_FRAMES = 25

/** Plays the PC microphone into the call by routing an AudioTrack to the telephony transmit device. */
class UplinkPlayer(private val context: Context, private val onFailure: (String) -> Unit) {
	private val queue = LinkedBlockingQueue<ByteArray>(QUEUE_FRAMES)
	@Volatile private var running = false
	private var thread: Thread? = null

	fun start() {
		if (running) return
		running = true
		thread = Thread(::run, "uplink-player").also { it.start() }
	}

	fun stop() {
		running = false
		thread?.join(1000)
		thread = null
		queue.clear()
	}

	/** Takes one frame of 48 kHz mono 16-bit little-endian PCM, drops the oldest when the player falls behind. */
	fun push(frame: ByteArray) {
		if (!running) return
		while (!queue.offer(frame)) queue.poll()
	}

	private fun run() {
		val audio = context.getSystemService(AudioManager::class.java)
		val devices = audio.getDevices(AudioManager.GET_DEVICES_OUTPUTS)
		Log.i("CallBridge", "output devices: " + devices.joinToString { "${it.type}:${it.productName}" })
		val transmit = devices.firstOrNull { it.type == AudioDeviceInfo.TYPE_TELEPHONY }
		if (transmit == null) {
			onFailure("PC microphone unavailable: no telephony output on this phone")
			return
		}
		val format = AudioFormat.Builder()
			.setEncoding(AudioFormat.ENCODING_PCM_16BIT)
			.setSampleRate(WIRE_RATE)
			.setChannelMask(AudioFormat.CHANNEL_OUT_MONO)
			.build()
		val minimum = AudioTrack.getMinBufferSize(WIRE_RATE, AudioFormat.CHANNEL_OUT_MONO, AudioFormat.ENCODING_PCM_16BIT)
		val track = AudioTrack.Builder()
			.setAudioAttributes(
				AudioAttributes.Builder()
					.setUsage(AudioAttributes.USAGE_VOICE_COMMUNICATION)
					.setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
					.build()
			)
			.setAudioFormat(format)
			.setBufferSizeInBytes(minimum * 2)
			.setTransferMode(AudioTrack.MODE_STREAM)
			.setPerformanceMode(AudioTrack.PERFORMANCE_MODE_LOW_LATENCY)
			.build()
		try {
			track.preferredDevice = transmit
			track.play()
			Log.i("CallBridge", "uplink routed to ${track.routedDevice?.type}:${track.routedDevice?.productName}")
			while (running) {
				val frame = queue.poll(100, TimeUnit.MILLISECONDS) ?: continue
				track.write(frame, 0, frame.size)
			}
		} finally {
			runCatching { track.stop() }
			track.release()
		}
	}
}
