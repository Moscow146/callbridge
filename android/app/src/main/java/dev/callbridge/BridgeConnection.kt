package dev.callbridge

import org.json.JSONObject
import java.io.IOException
import java.net.InetSocketAddress
import java.net.Socket

/** Connects to the PC, performs the handshake and dispatches incoming frames. */
class BridgeConnection(
	private val host: String,
	private val port: Int,
	private val code: String,
	private val deviceName: String,
	private val listener: Listener,
) {
	interface Listener {
		fun onConnected()
		fun onControl(message: JSONObject)
		fun onUplink(packet: ByteArray)
		fun onClosed(reason: String)
	}

	private var socket: Socket? = null
	private var frames: FrameStream? = null
	@Volatile private var closed = false

	fun start() {
		Thread(::run, "bridge-connection").start()
	}

	fun close() {
		closed = true
		runCatching { socket?.close() }
	}

	fun sendDownlink(packet: ByteArray) = send(FrameType.DOWNLINK, packet)

	fun sendControl(message: JSONObject) = send(FrameType.CONTROL, message.toString().toByteArray())

	private fun send(type: Int, payload: ByteArray) {
		val stream = frames ?: return
		try {
			stream.write(type, payload)
		} catch (e: IOException) {
			runCatching { socket?.close() }
		}
	}

	private fun run() {
		var reason = "closed"
		try {
			val s = Socket()
			socket = s
			s.tcpNoDelay = true
			s.connect(InetSocketAddress(host, port), 5000)
			val stream = FrameStream(s.getInputStream(), s.getOutputStream())
			val hello = JSONObject().put("version", PROTOCOL_VERSION).put("device", deviceName).put("token", code)
			stream.write(FrameType.HELLO, hello.toString().toByteArray())
			val ack = stream.read()
			if (ack == null || ack.type != FrameType.HELLO_ACK) throw IOException("No handshake reply")
			val reply = JSONObject(String(ack.payload))
			if (!reply.optBoolean("ok")) {
				reason = reply.optString("error", "rejected")
				return
			}
			frames = stream
			listener.onConnected()
			while (!closed) {
				val frame = stream.read() ?: break
				when (frame.type) {
					FrameType.CONTROL -> listener.onControl(JSONObject(String(frame.payload)))
					FrameType.UPLINK -> listener.onUplink(frame.payload)
					FrameType.PING -> stream.write(FrameType.PONG, frame.payload)
				}
			}
		} catch (e: IOException) {
			reason = e.message ?: "connection error"
		} finally {
			frames = null
			runCatching { socket?.close() }
			listener.onClosed(if (closed) "stopped" else reason)
		}
	}
}
