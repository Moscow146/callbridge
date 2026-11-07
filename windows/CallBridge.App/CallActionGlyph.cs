using System.Windows;

namespace CallBridge.App;

/// <summary>The icon a call toggle shows while it is on.</summary>
public static class CallActionGlyph
{
	public static readonly DependencyProperty CheckedProperty =
		DependencyProperty.RegisterAttached("Checked", typeof(string), typeof(CallActionGlyph));

	public static string? GetChecked(DependencyObject element) => (string?)element.GetValue(CheckedProperty);

	public static void SetChecked(DependencyObject element, string? value) => element.SetValue(CheckedProperty, value);
}
