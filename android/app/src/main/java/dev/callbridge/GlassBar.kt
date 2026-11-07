package dev.callbridge

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RenderEffect
import android.graphics.RenderNode
import android.graphics.Shader
import android.util.AttributeSet
import android.view.View
import android.widget.FrameLayout

class GlassBar @JvmOverloads constructor(context: Context, attrs: AttributeSet? = null) : FrameLayout(context, attrs) {
	var source: View? = null
		set(value) {
			field = value
			invalidate()
		}
	var blurEnabled = true
		set(value) {
			if (field == value) return
			field = value
			invalidate()
		}
	var amount = 1f
		set(value) {
			val clamped = value.coerceIn(0f, 1f)
			if (field == clamped) return
			field = clamped
			invalidate()
		}
	var edgeAtTop = false

	private val density = resources.displayMetrics.density
	private val margin = (48 * density).toInt()
	private val node = RenderNode("glass").apply {
		setRenderEffect(RenderEffect.createBlurEffect(24 * density, 24 * density, Shader.TileMode.CLAMP))
	}
	private val paint = Paint()
	private val here = IntArray(2)
	private val there = IntArray(2)
	private var keepDrawingUntil = 0L
	private val tick = Runnable { keepDrawing() }

	init {
		setWillNotDraw(false)
	}

	fun invalidateFor(millis: Long) {
		keepDrawingUntil = System.currentTimeMillis() + millis
		keepDrawing()
	}

	private fun keepDrawing() {
		invalidate()
		if (System.currentTimeMillis() < keepDrawingUntil) postOnAnimation(tick)
	}

	override fun onDetachedFromWindow() {
		removeCallbacks(tick)
		super.onDetachedFromWindow()
	}

	override fun onDraw(canvas: Canvas) {
		val backdrop = source
		val blurred = blurEnabled && backdrop != null && canvas.isHardwareAccelerated
		if (amount < 1f) {
			paint.color = context.getColor(R.color.bg_grouped)
			canvas.drawRect(0f, 0f, width.toFloat(), height.toFloat(), paint)
		}
		if (amount > 0f) {
			canvas.saveLayerAlpha(0f, 0f, width.toFloat(), height.toFloat(), (amount * 255).toInt())
			if (blurred && backdrop != null) drawGlass(canvas, backdrop) else drawSolid(canvas)
			drawEdge(canvas, blurred)
			canvas.restore()
		}
	}

	private fun drawSolid(canvas: Canvas) {
		paint.color = context.getColor(R.color.surface)
		canvas.drawRect(0f, 0f, width.toFloat(), height.toFloat(), paint)
	}

	private fun drawGlass(canvas: Canvas, backdrop: View) {
		getLocationInWindow(here)
		backdrop.getLocationInWindow(there)
		node.setPosition(0, 0, width + 2 * margin, height + 2 * margin)
		val recording = node.beginRecording()
		recording.drawColor(context.getColor(R.color.bg_grouped))
		recording.translate((there[0] - here[0] + margin).toFloat(), (there[1] - here[1] + margin).toFloat())
		backdrop.draw(recording)
		node.endRecording()
		canvas.drawColor(context.getColor(R.color.bg_grouped))
		canvas.save()
		canvas.clipRect(0, 0, width, height)
		canvas.translate(-margin.toFloat(), -margin.toFloat())
		canvas.drawRenderNode(node)
		canvas.restore()
		canvas.drawColor(context.getColor(R.color.glass_bar_tint))
	}

	private fun drawEdge(canvas: Canvas, glass: Boolean) {
		val y = if (edgeAtTop) 0f else height - 1f
		paint.color = context.getColor(if (glass) R.color.glass_edge else R.color.separator)
		canvas.drawRect(0f, y, width.toFloat(), y + 1f, paint)
		if (glass) {
			val inner = if (edgeAtTop) 1f else height - 2f
			paint.color = context.getColor(R.color.glass_stroke)
			canvas.drawRect(0f, inner, width.toFloat(), inner + 1f, paint)
		}
	}
}
