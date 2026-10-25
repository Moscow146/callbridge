package dev.callbridge

import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.io.DataInputStream
import java.io.IOException
import java.io.InputStream
import java.io.OutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

object FrameType {
	const val HELLO = 0x01
	const val HELLO_ACK = 0x02
	const val DOWNLINK = 0x10
	const val UPLINK = 0x11
	const val CONTROL = 0x20
	const val PING = 0x30
	const val PONG = 0x31
}

const val PROTOCOL_VERSION = 1
const val DEFAULT_PORT = 47800
private const val MAX_PAYLOAD = 1 shl 20

class Frame(val type: Int, val payload: ByteArray)

/** Length-prefixed frames, see docs/PROTOCOL.md. */
class FrameStream(input: InputStream, output: OutputStream) {
	private val input = DataInputStream(BufferedInputStream(input))
	private val output = BufferedOutputStream(output)
	private val writeLock = Any()

	fun write(type: Int, payload: ByteArray) {
		val header = ByteBuffer.allocate(5).order(ByteOrder.LITTLE_ENDIAN)
			.put(type.toByte()).putInt(payload.size).array()
		synchronized(writeLock) {
			output.write(header)
			output.write(payload)
			output.flush()
		}
	}

	/** Returns null when the peer closed the stream between frames. */
	fun read(): Frame? {
		val type = input.read()
		if (type < 0) return null
		val lengthBytes = ByteArray(4)
		input.readFully(lengthBytes)
		val length = ByteBuffer.wrap(lengthBytes).order(ByteOrder.LITTLE_ENDIAN).int
		if (length < 0 || length > MAX_PAYLOAD) throw IOException("Bad frame length $length")
		val payload = ByteArray(length)
		input.readFully(payload)
		return Frame(type, payload)
	}
}
