package dev.callbridge

import android.annotation.SuppressLint
import android.content.Context
import android.hardware.display.DisplayManager
import android.view.Display
import android.provider.Settings
import android.view.MotionEvent
import android.view.View
import android.view.Window
import android.view.WindowManager
import androidx.dynamicanimation.animation.DynamicAnimation
import androidx.dynamicanimation.animation.SpringAnimation
import androidx.dynamicanimation.animation.SpringForce

object Motion {
	fun enabled(context: Context): Boolean =
		Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) > 0f

	fun highestRefreshModeId(context: Context): Int {
		val display = runCatching { context.display }.getOrNull()
			?: context.getSystemService(DisplayManager::class.java)?.getDisplay(Display.DEFAULT_DISPLAY)
			?: return 0
		val current = display.mode
		return display.supportedModes
			.filter { it.physicalWidth == current.physicalWidth && it.physicalHeight == current.physicalHeight }
			.maxByOrNull { it.refreshRate }
			?.modeId ?: 0
	}

	fun requestHighRefresh(window: Window) {
		val id = highestRefreshModeId(window.context)
		if (id == 0) return
		val params = window.attributes
		params.preferredDisplayModeId = id
		window.attributes = params
	}

	fun requestHighRefresh(context: Context, params: WindowManager.LayoutParams) {
		val id = highestRefreshModeId(context)
		if (id != 0) params.preferredDisplayModeId = id
	}

	fun spring(view: View, property: DynamicAnimation.ViewProperty, target: Float, stiffness: Float = 380f, damping: Float = 0.78f, velocity: Float = 0f): SpringAnimation {
		@Suppress("UNCHECKED_CAST")
		val springs = (view.getTag(R.id.motion_springs) as? HashMap<Int, SpringAnimation>)
			?: HashMap<Int, SpringAnimation>().also { view.setTag(R.id.motion_springs, it) }
		val key = property.hashCode()
		val existing = springs[key]
		existing?.cancel()
		val animation = SpringAnimation(view, property).apply {
			spring = SpringForce(target).setStiffness(stiffness).setDampingRatio(damping)
			setStartVelocity(velocity)
		}
		springs[key] = animation
		animation.start()
		return animation
	}

	fun appear(view: View, delay: Long = 0, distance: Float = 24f, onDone: (() -> Unit)? = null) {
		val context = view.context
		if (!enabled(context)) {
			view.alpha = 1f
			view.translationY = 0f
			view.scaleX = 1f
			view.scaleY = 1f
			onDone?.invoke()
			return
		}
		val offset = distance * context.resources.displayMetrics.density
		view.alpha = 0f
		view.translationY = offset
		view.scaleX = 0.96f
		view.scaleY = 0.96f
		view.postDelayed({
			view.animate().alpha(1f).setDuration(220).start()
			spring(view, DynamicAnimation.TRANSLATION_Y, 0f, 300f, 0.82f)
			spring(view, DynamicAnimation.SCALE_X, 1f, 300f, 0.7f)
			spring(view, DynamicAnimation.SCALE_Y, 1f, 300f, 0.7f).addEndListener { _, _, _, _ -> onDone?.invoke() }
		}, delay)
	}

	fun pop(view: View) {
		if (!enabled(view.context)) return
		view.scaleX = 0.82f
		view.scaleY = 0.82f
		spring(view, DynamicAnimation.SCALE_X, 1f, 260f, 0.45f)
		spring(view, DynamicAnimation.SCALE_Y, 1f, 260f, 0.45f)
	}

	@SuppressLint("ClickableViewAccessibility")
	fun pressable(view: View, depth: Float = 0.96f) {
		view.setOnTouchListener { touched, event ->
			if (enabled(touched.context)) {
				when (event.actionMasked) {
					MotionEvent.ACTION_DOWN -> {
						spring(touched, DynamicAnimation.SCALE_X, depth, 900f, 0.9f)
						spring(touched, DynamicAnimation.SCALE_Y, depth, 900f, 0.9f)
					}
					MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
						spring(touched, DynamicAnimation.SCALE_X, 1f, 500f, 0.55f)
						spring(touched, DynamicAnimation.SCALE_Y, 1f, 500f, 0.55f)
					}
				}
			}
			false
		}
	}
}
