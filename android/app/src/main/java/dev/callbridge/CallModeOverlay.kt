package dev.callbridge

import android.annotation.SuppressLint
import android.content.Context
import android.graphics.PixelFormat
import android.provider.Settings
import android.view.Gravity
import android.view.MotionEvent
import android.view.ViewConfiguration
import android.view.WindowManager
import android.widget.FrameLayout
import android.widget.TextView
import kotlin.math.abs

class CallModeOverlay(private val context: Context, private val onToggle: () -> Unit) {
	private val windows = context.getSystemService(WindowManager::class.java)
	private val density = context.resources.displayMetrics.density
	private val layout = WindowManager.LayoutParams(
		WindowManager.LayoutParams.WRAP_CONTENT,
		WindowManager.LayoutParams.WRAP_CONTENT,
		WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
		WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED,
		PixelFormat.TRANSLUCENT,
	).apply {
		gravity = Gravity.CENTER
		y = (-35 * density).toInt()
	}
	private var pill: FrameLayout? = null
	private var label: TextView? = null

	fun show(mode: String) {
		if (!Settings.canDrawOverlays(context)) return
		val view = pill ?: create().also {
			Motion.requestHighRefresh(context, layout)
			windows.addView(it, layout)
			pill = it
			Motion.appear(it, 0, 16f)
		}
		val text = label ?: return
		text.setText(if (mode == CallMode.PC) R.string.mode_on_pc else R.string.mode_on_phone)
		val action = context.getString(if (mode == CallMode.PC) R.string.mode_move_to_phone else R.string.mode_move_to_pc)
		view.contentDescription = "${text.text}. $action"
	}

	fun hide() {
		val view = pill ?: return
		pill = null
		label = null
		if (!Motion.enabled(context)) {
			windows.removeView(view)
			return
		}
		view.animate().alpha(0f).scaleX(0.9f).scaleY(0.9f).setDuration(160).withEndAction {
			if (view.isAttachedToWindow) windows.removeView(view)
		}.start()
	}

	@SuppressLint("ClickableViewAccessibility")
	private fun create(): FrameLayout {
		val text = TextView(context).apply {
			setBackgroundResource(R.drawable.pill_overlay)
			setTextColor(context.getColor(R.color.glass_label))
			textSize = 15f
			gravity = Gravity.CENTER_VERTICAL
			minHeight = (44 * density).toInt()
			setCompoundDrawablesRelativeWithIntrinsicBounds(R.drawable.ic_swap, 0, 0, 0)
			compoundDrawablePadding = (8 * density).toInt()
			setPaddingRelative((14 * density).toInt(), (8 * density).toInt(), (18 * density).toInt(), (8 * density).toInt())
			elevation = 8 * density
		}
		SystemFont.apply(text)
		val margin = (16 * density).toInt()
		val view = FrameLayout(context).apply {
			setPadding(margin, margin, margin, margin)
			clipToPadding = false
			clipChildren = false
			isClickable = true
			addView(text, FrameLayout.LayoutParams(FrameLayout.LayoutParams.WRAP_CONTENT, FrameLayout.LayoutParams.WRAP_CONTENT))
			setOnClickListener { onToggle() }
		}
		label = text
		val slop = ViewConfiguration.get(context).scaledTouchSlop
		var startX = 0f
		var startY = 0f
		var originX = 0
		var originY = 0
		var dragging = false
		view.setOnTouchListener { touched, event ->
			when (event.action) {
				MotionEvent.ACTION_DOWN -> {
					startX = event.rawX
					startY = event.rawY
					originX = layout.x
					originY = layout.y
					dragging = false
				}
				MotionEvent.ACTION_MOVE -> {
					val dx = event.rawX - startX
					val dy = event.rawY - startY
					if (abs(dx) > slop || abs(dy) > slop) dragging = true
					if (dragging) {
						layout.x = originX + dx.toInt()
						layout.y = originY + dy.toInt()
						windows.updateViewLayout(touched, layout)
					}
				}
				MotionEvent.ACTION_UP -> if (!dragging) touched.performClick()
			}
			true
		}
		return view
	}
}
