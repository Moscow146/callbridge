package dev.callbridge

import android.content.Context
import android.graphics.Canvas
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.Path
import android.graphics.RenderEffect
import android.graphics.RenderNode
import android.graphics.Shader
import android.util.AttributeSet
import android.view.View
import android.view.ViewOutlineProvider
import android.graphics.Outline
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
	private val density = resources.displayMetrics.density
	private val margin = (48 * density).toInt()
	private val node = RenderNode("glass").apply {
		setRenderEffect(RenderEffect.createBlurEffect(24 * density, 24 * density, Shader.TileMode.CLAMP))
	}
	private val radius = 28 * density
	private val paint = Paint(Paint.ANTI_ALIAS_FLAG)
	private val clip = Path()
	private val ring = Path()
	private val stroke = 1.25f * density
	private val here = IntArray(2)
	private val there = IntArray(2)
	private var keepDrawingUntil = 0L
	private val tick = Runnable { keepDrawing() }

	init {
		setWillNotDraw(false)
		elevation = 10 * density
		outlineProvider = object : ViewOutlineProvider() {
			override fun getOutline(view: View, outline: Outline) {
				outline.setRoundRect(0, 0, view.width, view.height, radius)
			}
		}
		outlineSpotShadowColor = context.getColor(R.color.capsule_shadow)
		outlineAmbientShadowColor = context.getColor(R.color.capsule_shadow)
	}

	override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
		super.onSizeChanged(w, h, oldw, oldh)
		clip.reset()
		clip.addRoundRect(0f, 0f, w.toFloat(), h.toFloat(), radius, radius, Path.Direction.CW)
		val half = stroke / 2
		ring.reset()
		ring.addRoundRect(half, half, w - half, h - half, radius - half, radius - half, Path.Direction.CW)
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
		canvas.save()
		canvas.clipPath(clip)
		if (blurred && backdrop != null) drawGlass(canvas, backdrop) else drawSolid(canvas)
		canvas.restore()
		drawEdge(canvas, blurred)
	}

	private fun drawSolid(canvas: Canvas) {
		paint.shader = null
		paint.style = Paint.Style.FILL
		paint.color = context.getColor(R.color.surface)
		canvas.drawPath(clip, paint)
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
		canvas.translate(-margin.toFloat(), -margin.toFloat())
		canvas.drawRenderNode(node)
		canvas.restore()
		canvas.drawColor(context.getColor(R.color.capsule_tint))
		canvas.drawColor(context.getColor(R.color.capsule_scrim))
	}

	private fun drawEdge(canvas: Canvas, glass: Boolean) {
		paint.style = Paint.Style.STROKE
		paint.strokeWidth = stroke
		if (glass) {
			paint.shader = LinearGradient(
				0f, 0f, 0f, height.toFloat(),
				context.getColor(R.color.capsule_edge_top),
				context.getColor(R.color.capsule_edge_bottom),
				Shader.TileMode.CLAMP,
			)
		} else {
			paint.shader = null
			paint.color = context.getColor(R.color.separator)
		}
		canvas.drawPath(ring, paint)
		paint.shader = null
		paint.style = Paint.Style.FILL
	}
}
