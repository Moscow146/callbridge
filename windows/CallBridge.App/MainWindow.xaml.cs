using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
		VolumeText.Text = $"{settings.Volume * 100:0}%";
		loading = false;

		server.PhoneConnected += s => Dispatcher.Invoke(() => OnPhoneConnected(s));
		server.PhoneDisconnected += () => Dispatcher.Invoke(OnPhoneDisconnected);
		server.Start();

		levelTimer.Tick += (_, _) => MicLevel.Value = session?.MicLevel ?? 0;
		levelTimer.Start();
		Closed += (_, _) => server.Dispose();
	}

	[DllImport("dwmapi.dll")]
	static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		var handle = new WindowInteropHelper(this).Handle;
		var dark = 1;
		DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
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
		s.AudioRouteChanged += reason => Dispatcher.Invoke(() => ShowRoute(reason == null ? null : $"Call audio unavailable: {reason}"));
		StatusText.Text = $"Connected · {s.DeviceName}";
		StatusDot.Fill = (Brush)FindResource("Green");
		QrCard.Visibility = Visibility.Collapsed;
		DialButton.IsEnabled = true;
		ApplyDevices();
		s.Muted = MuteButton.IsChecked == true;
		s.Deafened = DeafenButton.IsChecked == true;
	}

	void OnPhoneDisconnected()
	{
		session = null;
		StatusText.Text = "Waiting for phone";
		StatusDot.Fill = (Brush)FindResource("Amber");
		QrCard.Visibility = Visibility.Visible;
		DialButton.IsEnabled = false;
		ShowState(new PhoneState(CallState.Idle, null, null));
		ShowRoute(null);
	}

	void ShowRoute(string? message)
	{
		RouteText.Text = message ?? "";
		RouteText.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
	}

	void ApplyDevices()
	{
		try
		{
			session?.SetDevices(SelectedId(MicCombo), SelectedId(SpeakerCombo), (float)VolumeSlider.Value);
		}
		catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException)
		{
			ShowRoute($"Audio device error: {e.Message}");
		}
	}

	void ShowState(PhoneState state)
	{
		var who = state.Name ?? state.Number ?? "Unknown number";
		var idle = state.Call == CallState.Idle;
		CallTitle.Text = idle ? "No call" : who;
		CallSub.Text = state.Call switch
		{
			CallState.Ringing => "Incoming call",
			CallState.Dialing => "Calling…",
			CallState.Active => "In call",
			CallState.Held => "On hold",
			_ => "Waiting for a call",
		};
		var ringing = state.Call == CallState.Ringing;
		AnswerButton.Visibility = ringing ? Visibility.Visible : Visibility.Collapsed;
		RejectButton.Visibility = ringing ? Visibility.Visible : Visibility.Collapsed;
		HangupButton.Visibility = state.Call is CallState.Active or CallState.Dialing or CallState.Held ? Visibility.Visible : Visibility.Collapsed;
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
		VolumeText.Text = $"{e.NewValue * 100:0}%";
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
