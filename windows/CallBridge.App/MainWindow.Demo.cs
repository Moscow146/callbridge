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
			"pairing" => 830,
			"pairingscroll" => 560,
			"ringing" => 690,
			"call" => 690,
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
		ShowState(scene switch
		{
			"ringing" => new PhoneState(CallState.Ringing, "+1 555 0100", "Anna Smith"),
			"call" or "callscroll" => new PhoneState(CallState.Active, "+1 555 0100", "Anna Smith"),
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

	void ScrollDemo(double offset)
	{
		if (offset > 0)
			Loaded += (_, _) => Dispatcher.BeginInvoke(() => Scroller.ScrollToVerticalOffset(offset), DispatcherPriority.ApplicationIdle);
	}
}
