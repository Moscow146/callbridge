package dev.callbridge

import android.graphics.Paint
import android.graphics.Typeface
import android.view.View
import android.view.ViewGroup
import android.widget.TextView

object SystemFont {
	private val families = listOf("SF Pro Text", "SF Pro Display", "SF Pro")
	private const val sample = "Hamburgefonstiv 0123456789"

	private val installed: String? by lazy {
		val reference = widthOf(Typeface.DEFAULT)
		families.firstOrNull { widthOf(Typeface.create(it, Typeface.NORMAL)) != reference }
	}

	private fun widthOf(typeface: Typeface): Float =
		Paint().apply {
			this.typeface = typeface
			textSize = 100f
		}.measureText(sample)

	fun apply(root: View) {
		val family = installed ?: return
		val base = Typeface.create(family, Typeface.NORMAL)
		applyTo(root, base)
	}

	private fun applyTo(view: View, base: Typeface) {
		if (view is TextView) {
			val weight = view.typeface?.weight ?: 400
			view.typeface = Typeface.create(base, weight, false)
		}
		if (view is ViewGroup) {
			for (index in 0 until view.childCount) applyTo(view.getChildAt(index), base)
		}
	}
}
