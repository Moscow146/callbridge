package dev.callbridge

import org.json.JSONException
import org.json.JSONObject
import java.io.IOException
import java.net.InetSocketAddress
import java.net.Socket
import java.util.concurrent.Executors

private const val CONNECT_TIMEOUT_MS = 1500

/** Connects to the PC, performs the handshake and dispatches incoming frames. */
class BridgeConnection(
	private val pairing: Pairing,
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
	private val sender = Executors.newSingleThreadExecutor()
	@Volatile private var closed = false

	fun start() {
		Thread(::run, "bridge-connection").start()
	}

	fun close() {
		closed = true
		sender.shutdown()
		runCatching { socket?.close() }
	}

	fun sendDownlink(packet: ByteArray) = send(FrameType.DOWNLINK, packet)

	fun sendControl(message: JSONObject) = send(FrameType.CONTROL, message.toString().toByteArray())

	/** Writes on a dedicated thread: callers run on the main thread or the audio thread. */
	private fun send(type: Int, payload: ByteArray) {
		if (sender.isShutdown) return
		sender.execute {
			val stream = frames ?: return@execute
			try {
				stream.write(type, payload)
			} catch (e: IOException) {
				runCatching { socket?.close() }
			}
		}
	}

	/** Tries every address from the QR code, the first that answers wins. */
	private fun reach(): Socket? {
		for (host in pairing.hosts) {
			if (closed) return null
			val s = Socket()
			try {
				s.tcpNoDelay = true
				s.connect(InetSocketAddress(host, pairing.port), CONNECT_TIMEOUT_MS)
				return s
			} catch (e: IOException) {
				runCatching { s.close() }
			}
		}
		return null
	}

	private fun run() {
		var reason = "closed"
		try {
			val s = reach() ?: throw IOException("PC not reachable")
			socket = s
			val stream = FrameStream(s.getInputStream(), s.getOutputStream())
			val hello = JSONObject().put("version", PROTOCOL_VERSION).put("device", deviceName).put("token", pairing.code)
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
		} catch (e: JSONException) {
			reason = "bad message from PC"
		} finally {
			frames = null
			sender.shutdown()
			runCatching { socket?.close() }
			if (!closed) listener.onClosed(reason)
		}
	}
}
