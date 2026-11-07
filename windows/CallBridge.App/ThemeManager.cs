using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CallBridge.Core.Appearance;
using Microsoft.Win32;

namespace CallBridge.App;

static class ThemeManager
{
	const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
	const string GlassKey = "GlassBar";
	const string GlassSolidKey = "GlassSolid";
	const string EdgeKey = "SectionEdge";
	const string EdgeMicaKey = "SectionEdgeMica";
	const int ImmersiveDarkMode = 20;
	const int CaptionColor = 35;
	const int SystemBackdropType = 38;
	const int BackdropNone = 1;
	const int BackdropMica = 2;
	const int DefaultCaptionColor = -1;

	static ResourceDictionary? current;
	static string? forcedTheme;
	static bool? forcedTransparency;
	static bool backdropFailed;

	public static WindowMaterial Material { get; private set; } = WindowMaterial.Solid;

	public static void Start(string? theme, bool? transparency = null)
	{
		forcedTheme = theme;
		forcedTransparency = transparency;
		Apply();
		SystemEvents.UserPreferenceChanged += (_, e) =>
		{
			if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility or UserPreferenceCategory.VisualStyle)
				Application.Current.Dispatcher.BeginInvoke(Apply);
		};
	}

	static bool SystemIsDark()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
			return key?.GetValue("AppsUseLightTheme") is 0;
		}
		catch (Exception e) when (e is System.Security.SecurityException or IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	static bool SystemTransparency()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
			return key?.GetValue("EnableTransparency") is not 0;
		}
		catch (Exception e) when (e is System.Security.SecurityException or IOException or UnauthorizedAccessException)
		{
			return true;
		}
	}

	static bool IsDark => forcedTheme == null ? SystemIsDark() : forcedTheme == "dark";

	static bool HighContrast => forcedTheme == null && SystemParameters.HighContrast;

	static bool TransparencyEnabled => forcedTransparency ?? SystemTransparency();

	static void Apply()
	{
		var name = HighContrast ? "HighContrast" : IsDark ? "Dark" : "Light";
		var resources = Application.Current.Resources;
		var merged = resources.MergedDictionaries;
		if (current != null)
			merged.Remove(current);
		resources.Remove(GlassKey);
		var dictionary = new ResourceDictionary { Source = new Uri($"Theme.{name}.xaml", UriKind.Relative) };
		merged.Insert(0, dictionary);
		current = dictionary;
		if (Application.Current.MainWindow != null)
			StyleWindow(Application.Current.MainWindow);
		else
		{
			Material = ChooseMaterial();
			ApplyMaterialResources();
		}
	}

	static WindowMaterial ChooseMaterial() =>
		MaterialPolicy.Choose(new MaterialContext(Environment.OSVersion.Version.Build, HighContrast, TransparencyEnabled, !backdropFailed));

	static void ApplyMaterialResources()
	{
		var resources = Application.Current.Resources;
		resources.Remove(GlassKey);
		resources.Remove(EdgeKey);
		if (MaterialPolicy.GlassLayer(Material))
		{
			if (resources[EdgeMicaKey] is Brush edge)
				resources[EdgeKey] = edge;
		}
		else if (resources[GlassSolidKey] is Brush solid)
			resources[GlassKey] = solid;
	}

	public static void StyleWindow(Window window)
	{
		var handle = new WindowInteropHelper(window).Handle;
		Material = ChooseMaterial();
		if (handle == IntPtr.Zero)
		{
			ApplyMaterialResources();
			return;
		}
		var dark = IsDark ? 1 : 0;
		TrySet(handle, ImmersiveDarkMode, dark);
		if (Material == WindowMaterial.Mica && !TryEnableMica(window, handle))
		{
			backdropFailed = true;
			Material = ChooseMaterial();
		}
		if (Material == WindowMaterial.Solid)
			ApplySolid(window, handle);
		ApplyMaterialResources();
	}

	static bool TryEnableMica(Window window, IntPtr handle)
	{
		try
		{
			if (!TrySet(handle, SystemBackdropType, BackdropMica))
				return false;
			TrySet(handle, CaptionColor, DefaultCaptionColor);
			var source = HwndSource.FromHwnd(handle);
			if (source?.CompositionTarget == null)
				return false;
			source.CompositionTarget.BackgroundColor = Colors.Transparent;
			var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
			if (DwmExtendFrameIntoClientArea(handle, ref margins) != 0)
				return false;
			window.Background = Brushes.Transparent;
			return true;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or Win32Exception or InvalidOperationException)
		{
			return false;
		}
	}

	static void ApplySolid(Window window, IntPtr handle)
	{
		try
		{
			TrySet(handle, SystemBackdropType, BackdropNone);
			var source = HwndSource.FromHwnd(handle);
			if (source?.CompositionTarget != null)
				source.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
			var margins = new Margins();
			DwmExtendFrameIntoClientArea(handle, ref margins);
			var color = ((SolidColorBrush)Application.Current.Resources["WindowBg"]).Color;
			TrySet(handle, CaptionColor, color.R | color.G << 8 | color.B << 16);
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or Win32Exception or InvalidOperationException)
		{
		}
		window.SetResourceReference(Window.BackgroundProperty, "WindowBg");
	}

	static bool TrySet(IntPtr handle, int attribute, int value)
	{
		try
		{
			return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) == 0;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			return false;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Margins
	{
		public int Left;
		public int Right;
		public int Top;
		public int Bottom;
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	[DllImport("dwmapi.dll")]
	static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
}
