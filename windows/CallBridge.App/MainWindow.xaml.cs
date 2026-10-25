using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CallBridge.Core.Audio;
using CallBridge.Core.Protocol;
using CallBridge.Core.Session;
using QRCoder;

namespace CallBridge.App;

public partial class MainWindow : Window
{
	readonly AppSettings settings = AppSettings.Load();
	readonly CallBridgeServer server;
	readonly DispatcherTimer levelTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
	PhoneSession? session;
	bool loading = true;

	public MainWindow()
	{
		InitializeComponent();
		if (settings.PairingCode == null)
		{
			settings.PairingCode = CallBridgeServer.GenerateCode();
			settings.Save();
		}
		server = new CallBridgeServer(settings.PairingCode);
		ShowQr();
		FillDevices(MicCombo, AudioDevices.Capture(), settings.MicId);
		FillDevices(SpeakerCombo, AudioDevices.Render(), settings.SpeakerId);
		VolumeSlider.Value = settings.Volume;
		loading = false;

		server.PhoneConnected += s => Dispatcher.Invoke(() => OnPhoneConnected(s));
		server.PhoneDisconnected += () => Dispatcher.Invoke(OnPhoneDisconnected);
		server.Start();

		levelTimer.Tick += (_, _) => MicLevel.Value = session?.MicLevel ?? 0;
		levelTimer.Start();
		Closed += (_, _) => server.Dispose();
	}

	void ShowQr()
	{
		var hosts = new[] { "127.0.0.1" }.Concat(PairingInfo.LocalAddresses());
		var uri = PairingInfo.BuildUri(settings.PairingCode!, CallBridgeServer.DefaultPort, hosts, Environment.MachineName);
		using var generator = new QRCodeGenerator();
		var png = new PngByteQRCode(generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M)).GetGraphic(10);
		var image = new BitmapImage();
		image.BeginInit();
		image.CacheOption = BitmapCacheOption.OnLoad;
		image.StreamSource = new MemoryStream(png);
		image.EndInit();
		QrImage.Source = image;
	}

	void OnNewCode(object sender, RoutedEventArgs e)
	{
		settings.PairingCode = CallBridgeServer.GenerateCode();
		settings.Save();
		server.PairingCode = settings.PairingCode;
		session?.Dispose();
		ShowQr();
	}

	static void FillDevices(ComboBox combo, IReadOnlyList<AudioDeviceInfo> devices, string? selectedId)
	{
		combo.ItemsSource = devices;
		combo.SelectedItem = devices.FirstOrDefault(d => d.Id == selectedId) ?? devices.FirstOrDefault();
	}

	string? SelectedId(ComboBox combo) => (combo.SelectedItem as AudioDeviceInfo)?.Id;

	void OnPhoneConnected(PhoneSession s)
	{
		session = s;
		s.StateChanged += state => Dispatcher.Invoke(() => ShowState(state));
		s.AudioRouteChanged += reason => Dispatcher.Invoke(() => RouteText.Text = reason == null ? "" : $"Call audio unavailable: {reason}");
		PhoneText.Text = $"Connected: {s.DeviceName}";
		PhoneText.Foreground = System.Windows.Media.Brushes.SeaGreen;
		DialButton.IsEnabled = true;
		ApplyDevices();
		s.Muted = MuteButton.IsChecked == true;
		s.Deafened = DeafenButton.IsChecked == true;
	}

	void OnPhoneDisconnected()
	{
		session = null;
		PhoneText.Text = "Phone not connected";
		PhoneText.Foreground = System.Windows.Media.Brushes.DarkOrange;
		DialButton.IsEnabled = false;
		ShowState(new PhoneState(CallState.Idle, null, null));
		RouteText.Text = "";
	}

	void ApplyDevices()
	{
		try
		{
			session?.SetDevices(SelectedId(MicCombo), SelectedId(SpeakerCombo), (float)VolumeSlider.Value);
		}
		catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException)
		{
			RouteText.Text = $"Audio device error: {e.Message}";
		}
	}

	void ShowState(PhoneState state)
	{
		var who = state.Name ?? state.Number;
		CallText.Text = state.Call switch
		{
			CallState.Ringing => $"Incoming: {who}",
			CallState.Dialing => $"Calling: {who}",
			CallState.Active => $"In call: {who}",
			CallState.Held => $"On hold: {who}",
			_ => "No call",
		};
		AnswerButton.IsEnabled = state.Call == CallState.Ringing;
		RejectButton.IsEnabled = state.Call == CallState.Ringing;
		HangupButton.IsEnabled = state.Call is CallState.Active or CallState.Dialing or CallState.Held;
	}

	void OnDeviceChanged(object sender, SelectionChangedEventArgs e)
	{
		if (loading)
			return;
		settings.MicId = SelectedId(MicCombo);
		settings.SpeakerId = SelectedId(SpeakerCombo);
		settings.Save();
		ApplyDevices();
	}

	void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (loading)
			return;
		settings.Volume = e.NewValue;
		settings.Save();
		if (session != null)
			session.SpeakerVolume = (float)e.NewValue;
	}

	void OnMuteClick(object sender, RoutedEventArgs e)
	{
		if (session != null)
			session.Muted = MuteButton.IsChecked == true;
	}

	void OnDeafenClick(object sender, RoutedEventArgs e)
	{
		if (session != null)
			session.Deafened = DeafenButton.IsChecked == true;
	}

	void OnAnswer(object sender, RoutedEventArgs e) => _ = session?.SendCommandAsync("answer");

	void OnReject(object sender, RoutedEventArgs e) => _ = session?.SendCommandAsync("reject");

	void OnHangup(object sender, RoutedEventArgs e) => _ = session?.SendCommandAsync("hangup");

	void OnDial(object sender, RoutedEventArgs e)
	{
		var number = DialBox.Text.Trim();
		if (number.Length > 0)
			_ = session?.SendCommandAsync("dial", new { Number = number });
	}

	void OnDialKey(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter && DialButton.IsEnabled)
			OnDial(sender, e);
	}
}
