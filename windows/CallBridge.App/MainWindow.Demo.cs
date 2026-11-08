using System.Windows;
using System.Windows.Threading;
using CallBridge.Core.Audio;
using CallBridge.Core.Protocol;

namespace CallBridge.App;

/// <summary>Invented devices for `CallBridge.exe --demo`, so screenshots show no real hardware.</summary>
static class DemoDevices
{
	public static readonly IReadOnlyList<AudioDeviceInfo> Microphones =
	[
		new("mic1", "Studio Microphone"),
		new("mic2", "Headset Microphone"),
	];

	public static readonly IReadOnlyList<AudioDeviceInfo> Outputs =
	[
		new("out1", "Headphones"),
		new("out2", "Speakers"),
	];
}

public partial class MainWindow
{
	void ShowDemo(string scene)
	{
		Height = scene switch
		{
			"pairing" => 960,
			"pairingscroll" => 560,
			"ringing" => 960,
			"call" => 1020,
			"keypad" => 900,
			"callscroll" => 540,
			_ => 660,
		};
		Topmost = true;
		ShowActivated = scene == "suggest";
		WindowStartupLocation = WindowStartupLocation.Manual;
		Top = 0;
		Left = 100;
		VolumeSlider.Value = 1;
		MicLevel.Value = 0.55;
		if (scene.StartsWith("pairing"))
		{
			ScrollDemo(scene == "pairingscroll" ? 150 : 0);
			return;
		}

		StatusText.Text = Strings.Format("ConnectedTo", "Pixel 9");
		StatusDot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "Green");
		QrCard.Visibility = Visibility.Collapsed;
		CallCard.Visibility = Visibility.Visible;
		DialButton.IsEnabled = true;
		contacts =
		[
			new("Anna Smith", "+1 555 0100"),
			new("Andrew Miller", "+1 555 0142"),
			new("Hannah Brown", "+1 555 0177"),
			new("Daniel Carter", "+1 555 0199"),
		];
		photos.Offer("+1 555 0100", DemoPhoto(0x3A7BD5, 0x00D2FF, false));
		photos.Offer("+1 555 0199", DemoPhoto(0xF2994A, 0xF2C94C, true));
		if (scene == "keypad")
			KeypadButton.IsChecked = true;
		ShowState(scene switch
		{
			"ringing" => new PhoneState(CallState.Ringing, "+1 555 0100", "Anna Smith"),
			"call" or "callscroll" => new PhoneState(CallState.Active, "+1 555 0100", "Anna Smith"),
			"keypad" => new PhoneState(CallState.Held, "+1 555 0100", "Anna Smith"),
			_ => new PhoneState(CallState.Idle, null, null),
		});
		ScrollDemo(scene == "callscroll" ? 120 : 0);
		if (scene == "suggest")
		{
			Topmost = true;
			Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
			{
				Activate();
				DialBox.Focus();
				DialBox.Text = "an";
			}, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
		}
	}

	static byte[] DemoPhoto(int from, int to, bool sun)
	{
		var visual = new System.Windows.Media.DrawingVisual();
		using (var dc = visual.RenderOpen())
		{
			System.Windows.Media.Color C(int rgb) => System.Windows.Media.Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
			dc.DrawRectangle(new System.Windows.Media.LinearGradientBrush(C(from), C(to), 45), null, new Rect(0, 0, 96, 96));
			var ink = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 255, 255, 255));
			if (sun)
			{
				dc.DrawEllipse(ink, null, new Point(48, 40), 18, 18);
				dc.DrawRoundedRectangle(ink, null, new Rect(16, 66, 64, 40), 20, 20);
			}
			else
			{
				dc.DrawEllipse(ink, null, new Point(48, 38), 17, 17);
				dc.DrawEllipse(ink, null, new Point(48, 100), 38, 30);
			}
		}
		var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(96, 96, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
		bitmap.Render(visual);
		var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder { QualityLevel = 80 };
		encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
		using var stream = new System.IO.MemoryStream();
		encoder.Save(stream);
		return stream.ToArray();
	}

	void ScrollDemo(double offset)
	{
		if (offset > 0)
			Loaded += (_, _) => Dispatcher.BeginInvoke(() => Scroller.ScrollToVerticalOffset(offset), DispatcherPriority.ApplicationIdle);
	}
}
