package dev.callbridge

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class DtmfTest {
	@Test
	fun acceptsKeypadSymbols() {
		for (c in "0123456789*#") assertEquals(c, Dtmf.parse(c.toString()))
	}

	@Test
	fun upperCasesLetters() {
		assertEquals('A', Dtmf.parse("a"))
		assertEquals('D', Dtmf.parse("D"))
	}

	@Test
	fun rejectsEverythingElse() {
		assertNull(Dtmf.parse(null))
		assertNull(Dtmf.parse(""))
		assertNull(Dtmf.parse("12"))
		assertNull(Dtmf.parse("E"))
		assertNull(Dtmf.parse(" "))
		assertNull(Dtmf.parse("+"))
	}
}
