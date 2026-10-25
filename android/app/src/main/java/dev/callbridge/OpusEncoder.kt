package dev.callbridge

import android.media.MediaCodec
import android.media.MediaCodecInfo
import android.media.MediaFormat

const val WIRE_RATE = 48000
const val FRAME_SAMPLES = WIRE_RATE / 50
private const val BITRATE = 32000

/** Opus encoder backed by the platform MediaCodec, 48 kHz mono, 20 ms frames. */
class OpusEncoder {
	private val codec = MediaCodec.createEncoderByType(MediaFormat.MIMETYPE_AUDIO_OPUS)
	private val info = MediaCodec.BufferInfo()
	private var ptsUs = 0L

	init {
		val format = MediaFormat.createAudioFormat(MediaFormat.MIMETYPE_AUDIO_OPUS, WIRE_RATE, 1)
		format.setInteger(MediaFormat.KEY_BIT_RATE, BITRATE)
		format.setInteger(MediaFormat.KEY_PCM_ENCODING, android.media.AudioFormat.ENCODING_PCM_16BIT)
		format.setInteger(MediaFormat.KEY_COMPLEXITY, 5)
		format.setInteger(MediaFormat.KEY_BITRATE_MODE, MediaCodecInfo.EncoderCapabilities.BITRATE_MODE_CBR)
		codec.configure(format, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE)
		codec.start()
	}

	/** Encodes one 20 ms frame and hands every finished packet to [sink]. */
	fun encode(pcm: ShortArray, sink: (ByteArray) -> Unit) {
		val index = codec.dequeueInputBuffer(20_000)
		if (index >= 0) {
			val buffer = codec.getInputBuffer(index)!!
			buffer.clear()
			buffer.order(java.nio.ByteOrder.nativeOrder()).asShortBuffer().put(pcm)
			codec.queueInputBuffer(index, 0, pcm.size * 2, ptsUs, 0)
			ptsUs += 1_000_000L * pcm.size / WIRE_RATE
		}
		drain(sink)
	}

	private fun drain(sink: (ByteArray) -> Unit) {
		while (true) {
			val index = codec.dequeueOutputBuffer(info, 0)
			when {
				index >= 0 -> {
					if (info.flags and MediaCodec.BUFFER_FLAG_CODEC_CONFIG == 0 && info.size > 0) {
						val buffer = codec.getOutputBuffer(index)!!
						buffer.position(info.offset)
						val data = ByteArray(info.size)
						buffer.get(data)
						sink(data)
					}
					codec.releaseOutputBuffer(index, false)
				}
				index == MediaCodec.INFO_TRY_AGAIN_LATER -> return
			}
		}
	}

	fun release() {
		runCatching { codec.stop() }
		codec.release()
	}
}
