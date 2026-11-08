package dev.callbridge

import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertEquals
import org.junit.Test

class ThumbnailTest {
	@Test
	fun cropsLandscapeToCenteredSquare() {
		assertArrayEquals(intArrayOf(50, 0, 100, 100), Thumbnail.cropSquare(200, 100))
	}

	@Test
	fun cropsPortraitToCenteredSquare() {
		assertArrayEquals(intArrayOf(0, 30, 60, 60), Thumbnail.cropSquare(60, 120))
	}

	@Test
	fun keepsSmallImagesAtFullSize() {
		assertEquals(1, Thumbnail.sampleSize(96, 96, 96))
		assertEquals(1, Thumbnail.sampleSize(150, 300, 96))
	}

	@Test
	fun halvesLargeImagesWhileStillAtLeastTarget() {
		assertEquals(4, Thumbnail.sampleSize(400, 800, 96))
		assertEquals(8, Thumbnail.sampleSize(768, 768, 96))
	}

	@Test
	fun triesDecreasingQualities() {
		val q = Thumbnail.qualities()
		assertEquals(80, q.first())
		assertEquals(q.sortedDescending(), q.toList())
	}
}
