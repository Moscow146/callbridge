package dev.callbridge

import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder

private val RATES = intArrayOf(48000, 16000, 8000)

/** Records the remote side of the call (needs CAPTURE_AUDIO_OUTPUT) and emits Opus packets. */
class DownlinkCapture(private val sink: (ByteArray) -> Unit, private val onFailure: (String) -> Unit) {
	@Volatile private var running = false
	private var thread: Thread? = null

	fun start() {
		if (running) return
		running = true
		thread = Thread(::run, "downlink-capture").also { it.start() }
	}

	fun stop() {
		running = false
		thread?.join(1000)
		thread = null
	}

	private fun open(): Pair<AudioRecord, Int>? {
		for (rate in RATES) {
			val min = AudioRecord.getMinBufferSize(rate, AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT)
			if (min <= 0) continue
			val record = try {
				AudioRecord.Builder()
					.setAudioSource(MediaRecorder.AudioSource.VOICE_DOWNLINK)
					.setAudioFormat(
						AudioFormat.Builder()
							.setEncoding(AudioFormat.ENCODING_PCM_16BIT)
							.setSampleRate(rate)
							.setChannelMask(AudioFormat.CHANNEL_IN_MONO)
							.build()
					)
					.setBufferSizeInBytes(min * 4)
					.build()
			} catch (e: UnsupportedOperationException) {
				null
			} catch (e: IllegalArgumentException) {
				null
			}
			if (record != null && record.state == AudioRecord.STATE_INITIALIZED) return record to rate
			record?.release()
		}
		return null
	}

	private fun run() {
		val (record, rate) = open() ?: run {
			onFailure("VOICE_DOWNLINK source is unavailable (needs system privileges)")
			return
		}
		val encoder = OpusEncoder()
		val resampler = LinearResampler(rate)
		val chunk = ShortArray(rate / 50)
		val pending = ShortArray(FRAME_SAMPLES * 4)
		var pendingSize = 0
		try {
			record.startRecording()
			if (record.recordingState != AudioRecord.RECORDSTATE_RECORDING) {
				onFailure("Call audio recording did not start")
				return
			}
			while (running) {
				val read = record.read(chunk, 0, chunk.size)
				if (read <= 0) {
					if (read < 0) {
						onFailure("Call audio read error $read")
						return
					}
					continue
				}
				val resampled = resampler.process(chunk, read)
				resampled.copyInto(pending, pendingSize)
				pendingSize += resampled.size
				while (pendingSize >= FRAME_SAMPLES) {
					encoder.encode(pending.copyOf(FRAME_SAMPLES), sink)
					pending.copyInto(pending, 0, FRAME_SAMPLES, pendingSize)
					pendingSize -= FRAME_SAMPLES
				}
			}
		} finally {
			runCatching { record.stop() }
			record.release()
			encoder.release()
		}
	}
}
