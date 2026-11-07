using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CallBridge.Core.Appearance;

namespace CallBridge.App;

static class WindowBackdrop
{
	const int CaptionColor = 35;
	const int SystemBackdropType = 38;
	const int BackdropNone = 1;
	const int BackdropMainWindow = 2;
	const int ColorDefault = -1;

	public static bool TryApply(Window window, WindowMaterial material, Color solidCaption)
	{
		var handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
			return material == WindowMaterial.Solid;
		var mica = material == WindowMaterial.Mica;
		try
		{
			var backdrop = mica ? BackdropMainWindow : BackdropNone;
			if (DwmSetWindowAttribute(handle, SystemBackdropType, ref backdrop, sizeof(int)) != 0)
			{
				if (mica)
					Reset(handle, solidCaption);
				return !mica;
			}
			var caption = mica ? ColorDefault : solidCaption.R | solidCaption.G << 8 | solidCaption.B << 16;
			DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(int));
			var margins = mica ? new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 } : default;
			if (DwmExtendFrameIntoClientArea(handle, ref margins) != 0)
			{
				Reset(handle, solidCaption);
				return !mica;
			}
			if (HwndSource.FromHwnd(handle)?.CompositionTarget is { } target)
				target.BackgroundColor = mica ? Colors.Transparent : Colors.White;
			return true;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
		{
			return !mica;
		}
	}

	static void Reset(IntPtr handle, Color solidCaption)
	{
		try
		{
			var none = BackdropNone;
			DwmSetWindowAttribute(handle, SystemBackdropType, ref none, sizeof(int));
			var caption = solidCaption.R | solidCaption.G << 8 | solidCaption.B << 16;
			DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(int));
			var margins = default(Margins);
			DwmExtendFrameIntoClientArea(handle, ref margins);
			if (HwndSource.FromHwnd(handle)?.CompositionTarget is { } target)
				target.BackgroundColor = Colors.White;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
		{
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Margins
	{
		public int Left, Right, Top, Bottom;
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	[DllImport("dwmapi.dll")]
	static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
}
