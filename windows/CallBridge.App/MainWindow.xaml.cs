using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CallBridge.Core.Audio;
using CallBridge.Core.Protocol;
using CallBridge.Core.Session;

namespace CallBridge.App;

public partial class MainWindow : Window
{
	readonly AppSettings settings = AppSettings.Load();
	readonly CallBridgeServer server = new();
	readonly DispatcherTimer levelTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
	PhoneSession? session;
	bool loading = true;

	public MainWindow()
	{
		InitializeComponent();
		CodeText.Text = server.PairingCode;
		AddressText.Text = DescribeAddresses();
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

	string DescribeAddresses()
	{
		var addresses = NetworkInterface.GetAllNetworkInterfaces()
			.Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
			.SelectMany(n => n.GetIPProperties().UnicastAddresses)
			.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
			.Select(a => a.Address.ToString());
		return $"Port {server.Port}. Address: {string.Join(", ", addresses)}. Over USB: adb reverse tcp:{server.Port} tcp:{server.Port}";
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
