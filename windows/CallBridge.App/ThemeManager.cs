using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace CallBridge.App;

/// <summary>Follows the Windows light, dark and high contrast settings.</summary>
static class ThemeManager
{
	const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
	const int ImmersiveDarkMode = 20;
	const int CaptionColor = 35;

	static ResourceDictionary? current;
	static string? forced;

	public static void Start(string? forcedTheme)
	{
		forced = forcedTheme;
		Apply();
		if (forced == null)
			SystemEvents.UserPreferenceChanged += (_, e) =>
			{
				if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility)
					Application.Current.Dispatcher.BeginInvoke(Apply);
			};
	}

	static bool SystemIsDark()
	{
		using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
		return key?.GetValue("AppsUseLightTheme") is 0;
	}

	static bool IsDark => forced == null ? SystemIsDark() : forced == "dark";

	static void Apply()
	{
		var name = forced == null && SystemParameters.HighContrast ? "HighContrast" : IsDark ? "Dark" : "Light";
		var dictionary = new ResourceDictionary { Source = new Uri($"Theme.{name}.xaml", UriKind.Relative) };
		var merged = Application.Current.Resources.MergedDictionaries;
		if (current != null)
			merged.Remove(current);
		merged.Insert(0, dictionary);
		current = dictionary;
		if (Application.Current.MainWindow != null)
			StyleTitleBar(Application.Current.MainWindow);
	}

	public static void StyleTitleBar(Window window)
	{
		var handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
			return;
		var dark = IsDark ? 1 : 0;
		DwmSetWindowAttribute(handle, ImmersiveDarkMode, ref dark, sizeof(int));
		var color = ((SolidColorBrush)Application.Current.Resources["WindowBg"]).Color;
		var colorRef = color.R | color.G << 8 | color.B << 16;
		DwmSetWindowAttribute(handle, CaptionColor, ref colorRef, sizeof(int));
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
