package dev.callbridge

import org.junit.Assert.assertEquals
import org.junit.Test

class NetworkReportTest {
	@Test
	fun namesSingleTransports() {
		assertEquals("wifi", NetworkReport.label(wifi = true, cellular = false, vpn = false, ethernet = false))
		assertEquals("cellular", NetworkReport.label(wifi = false, cellular = true, vpn = false, ethernet = false))
	}

	@Test
	fun showsVpnOverWifi() {
		assertEquals("wifi+vpn", NetworkReport.label(wifi = true, cellular = false, vpn = true, ethernet = false))
	}

	@Test
	fun reportsUnknownWhenNothingIsSet() {
		assertEquals("unknown", NetworkReport.label(wifi = false, cellular = false, vpn = false, ethernet = false))
	}
}
