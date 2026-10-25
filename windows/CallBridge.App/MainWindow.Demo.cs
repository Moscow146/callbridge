using System.Windows;
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
		Height = scene == "pairing" ? 1010 : 740;
		WindowStartupLocation = WindowStartupLocation.Manual;
		Top = 0;
		Left = 100;
		VolumeSlider.Value = 1;
		MicLevel.Value = 0.55;
		if (scene == "pairing")
			return;

		StatusText.Text = "Connected · Pixel 9";
		StatusDot.Fill = (System.Windows.Media.Brush)FindResource("Green");
		QrCard.Visibility = Visibility.Collapsed;
		DialButton.IsEnabled = true;
		ShowState(scene switch
		{
			"ringing" => new PhoneState(CallState.Ringing, "+1 555 0100", "Anna Smith"),
			"call" => new PhoneState(CallState.Active, "+1 555 0100", "Anna Smith"),
			_ => new PhoneState(CallState.Idle, null, null),
		});
	}
}
