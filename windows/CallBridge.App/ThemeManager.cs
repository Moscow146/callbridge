using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CallBridge.Core.Appearance;
using Microsoft.Win32;

namespace CallBridge.App;

/// <summary>Follows the Windows light, dark and high contrast settings.</summary>
static class ThemeManager
{
	const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
	const int ImmersiveDarkMode = 20;

	static ResourceDictionary? current;
	static string? forced;
	static bool forceOpaque;
	static bool backdropFailed;
	static WindowMaterial material = WindowMaterial.Solid;

	public static WindowMaterial Material => material;

	public static void Start(string? forcedTheme, bool opaque = false)
	{
		forced = forcedTheme;
		forceOpaque = opaque;
		Apply();
		SystemEvents.UserPreferenceChanged += (_, e) =>
		{
			if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility or UserPreferenceCategory.VisualStyle)
				Application.Current.Dispatcher.BeginInvoke(Apply);
		};
	}

	static bool SystemIsDark()
	{
		using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
		return key?.GetValue("AppsUseLightTheme") is 0;
	}

	static bool TransparencyEnabled()
	{
		using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
		return key?.GetValue("EnableTransparency") is not 0;
	}

	static bool IsDark => forced == null ? SystemIsDark() : forced == "dark";

	static WindowMaterial ChooseMaterial()
	{
		if (backdropFailed)
			return WindowMaterial.Solid;
		var transparency = !forceOpaque && TransparencyEnabled();
		return MaterialPolicy.Choose(new DisplaySettings(SystemParameters.HighContrast, transparency, Environment.OSVersion.Version.Build));
	}

	static void Apply()
	{
		var name = forced == null && SystemParameters.HighContrast ? "HighContrast" : IsDark ? "Dark" : "Light";
		var dictionary = new ResourceDictionary { Source = new Uri($"Theme.{name}.xaml", UriKind.Relative) };
		var merged = Application.Current.Resources.MergedDictionaries;
		if (current != null)
			merged.Remove(current);
		merged.Insert(0, dictionary);
		current = dictionary;
		material = ChooseMaterial();
		UpdateMaterialResources();
		if (Application.Current.MainWindow != null)
			StyleTitleBar(Application.Current.MainWindow);
	}

	static void UpdateMaterialResources()
	{
		var resources = Application.Current.Resources;
		var mica = material == WindowMaterial.Mica;
		resources["GlassBar"] = mica ? resources["GlassTint"] : resources["GlassSolid"];
		resources["WindowBackdrop"] = mica ? Brushes.Transparent : resources["WindowBg"];
	}

	public static void StyleTitleBar(Window window)
	{
		var handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
			return;
		var dark = IsDark ? 1 : 0;
		DwmSetWindowAttribute(handle, ImmersiveDarkMode, ref dark, sizeof(int));
		var color = ((SolidColorBrush)Application.Current.Resources["WindowBg"]).Color;
		if (!WindowBackdrop.TryApply(window, material, color))
		{
			backdropFailed = true;
			material = WindowMaterial.Solid;
			UpdateMaterialResources();
		}
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
