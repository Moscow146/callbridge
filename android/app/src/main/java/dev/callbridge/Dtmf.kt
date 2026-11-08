package dev.callbridge

object Dtmf {
	private const val DIGITS = "0123456789*#ABCD"

	fun parse(text: String?): Char? {
		if (text == null || text.length != 1) return null
		val digit = text[0].uppercaseChar()
		return if (DIGITS.indexOf(digit) >= 0) digit else null
	}
}
