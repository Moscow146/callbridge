package dev.callbridge

import android.annotation.SuppressLint
import android.content.Context
import android.graphics.PixelFormat
import android.provider.Settings
import android.view.Gravity
import android.view.MotionEvent
import android.view.ViewConfiguration
import android.view.WindowManager
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
	private var pill: TextView? = null

	fun show(mode: String) {
		if (!Settings.canDrawOverlays(context)) return
		val view = pill ?: create().also {
			windows.addView(it, layout)
			pill = it
		}
		view.setText(if (mode == CallMode.PC) R.string.mode_on_pc else R.string.mode_on_phone)
	}

	fun hide() {
		pill?.let { windows.removeView(it) }
		pill = null
	}

	@SuppressLint("ClickableViewAccessibility")
	private fun create(): TextView {
		val view = TextView(context).apply {
			setBackgroundResource(R.drawable.pill_overlay)
			setTextColor(0xFFF5F7FF.toInt())
			textSize = 14f
			val horizontal = (18 * density).toInt()
			val vertical = (10 * density).toInt()
			setPadding(horizontal, vertical, horizontal, vertical)
		}
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
				MotionEvent.ACTION_UP -> if (!dragging) onToggle()
			}
			true
		}
		return view
	}
}
