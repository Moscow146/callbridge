package dev.callbridge

/** Streaming linear resampler from [inRate] to [WIRE_RATE], mono. */
class LinearResampler(private val inRate: Int) {
	private var position = 0.0
	private var previous = 0

	fun process(input: ShortArray, length: Int): ShortArray {
		if (inRate == WIRE_RATE) return input.copyOf(length)
		val step = inRate.toDouble() / WIRE_RATE
		val out = ArrayList<Short>((length / step).toInt() + 2)
		while (position < length) {
			val index = position.toInt()
			val fraction = position - index
			val a = if (index == 0) previous else input[index - 1].toInt()
			val b = input[index].toInt()
			out.add((a + (b - a) * fraction).toInt().toShort())
			position += step
		}
		position -= length
		previous = input[length - 1].toInt()
		return out.toShortArray()
	}
}
