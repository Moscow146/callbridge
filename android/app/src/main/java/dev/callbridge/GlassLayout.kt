package dev.callbridge

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Shader
import android.util.AttributeSet
import android.widget.LinearLayout

private const val RADIUS_DP = 30f
private const val RIM_DP = 1.2f

/** Translucent card with a light-catching gradient rim, the glass surface of the UI. */
class GlassLayout @JvmOverloads constructor(context: Context, attrs: AttributeSet? = null) : LinearLayout(context, attrs) {
	private val density = resources.displayMetrics.density
	private val fill = Paint(Paint.ANTI_ALIAS_FLAG)
	private val rim = Paint(Paint.ANTI_ALIAS_FLAG).apply {
		style = Paint.Style.STROKE
		strokeWidth = RIM_DP * density
	}
	private val bounds = RectF()

	init {
		setWillNotDraw(false)
		orientation = VERTICAL
	}

	override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
		super.onSizeChanged(w, h, oldw, oldh)
		val inset = rim.strokeWidth / 2
		bounds.set(inset, inset, w - inset, h - inset)
		fill.shader = LinearGradient(
			0f, 0f, w.toFloat(), h.toFloat(),
			intArrayOf(Color.argb(61, 255, 255, 255), Color.argb(20, 255, 255, 255), Color.argb(31, 255, 255, 255)),
			floatArrayOf(0f, 0.55f, 1f), Shader.TileMode.CLAMP
		)
		rim.shader = LinearGradient(
			0f, 0f, w.toFloat(), h.toFloat(),
			intArrayOf(Color.argb(179, 255, 255, 255), Color.argb(31, 255, 255, 255), Color.argb(15, 255, 255, 255), Color.argb(102, 255, 255, 255)),
			floatArrayOf(0f, 0.35f, 0.65f, 1f), Shader.TileMode.CLAMP
		)
	}

	override fun onDraw(canvas: Canvas) {
		val radius = RADIUS_DP * density
		canvas.drawRoundRect(bounds, radius, radius, fill)
		canvas.drawRoundRect(bounds, radius, radius, rim)
		super.onDraw(canvas)
	}
}
