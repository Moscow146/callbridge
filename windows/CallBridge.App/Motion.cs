using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CallBridge.App;

static class Motion
{
	const int FrameRate = 240;
	static readonly IEasingFunction EaseOut = new ExponentialEase { Exponent = 5, EasingMode = EasingMode.EaseOut };
	static readonly IEasingFunction Spring = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseOut };

	public static bool Enabled => SystemParameters.ClientAreaAnimation;

	public static void Start()
	{
		Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(FrameRate));
		EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((s, _) => Press((ButtonBase)s, 0.94)), true);
		EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((s, _) => Release((ButtonBase)s)), true);
		EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.LostMouseCaptureEvent, new MouseEventHandler((s, _) => Release((ButtonBase)s)), true);
		EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
		{
			if (e.Key is Key.Space or Key.Enter && !e.IsRepeat)
				Press((ButtonBase)s, 0.94);
		}), true);
		EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.PreviewKeyUpEvent, new KeyEventHandler((s, _) => Release((ButtonBase)s)), true);
	}

	public static void Show(UIElement element, bool visible, double distance = 10)
	{
		var target = visible ? Visibility.Visible : Visibility.Collapsed;
		if (element.Visibility == target)
			return;
		element.Visibility = target;
		if (visible)
			Appear(element, distance);
	}

	public static void Appear(UIElement element, double distance = 10)
	{
		if (!Enabled)
			return;
		var move = new TranslateTransform(0, distance);
		element.RenderTransform = move;
		element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)) { EasingFunction = EaseOut, FillBehavior = FillBehavior.Stop });
		move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(distance, 0, TimeSpan.FromMilliseconds(420)) { EasingFunction = EaseOut });
	}

	public static void Refresh(UIElement element)
	{
		if (!Enabled)
			return;
		element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = EaseOut, FillBehavior = FillBehavior.Stop });
	}

	static ScaleTransform? PressScale(ButtonBase button, bool create)
	{
		if (VisualTreeHelper.GetChildrenCount(button) == 0 || VisualTreeHelper.GetChild(button, 0) is not FrameworkElement root)
			return null;
		if (root.RenderTransform is ScaleTransform existing && !existing.IsFrozen)
			return existing;
		if (!create)
			return null;
		var scale = new ScaleTransform(1, 1);
		root.RenderTransformOrigin = new Point(0.5, 0.5);
		root.RenderTransform = scale;
		return scale;
	}

	static void Press(ButtonBase button, double depth)
	{
		if (!Enabled || !button.IsEnabled)
			return;
		var scale = PressScale(button, true);
		if (scale == null)
			return;
		var animation = new DoubleAnimation(depth, TimeSpan.FromMilliseconds(70)) { EasingFunction = EaseOut };
		scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
		scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
	}

	static void Release(ButtonBase button)
	{
		var scale = PressScale(button, false);
		if (scale == null)
			return;
		if (!Enabled)
		{
			scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
			scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
			scale.ScaleX = scale.ScaleY = 1;
			return;
		}
		var animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(380)) { EasingFunction = Spring };
		scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
		scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
	}
}
