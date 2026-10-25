using System.IO;
using System.Net.Sockets;
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
	readonly AppSettings settings;
	readonly CallBridgeServer server;
	readonly DispatcherTimer levelTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
	readonly bool demo;
	IReadOnlyList<Contact> contacts = [];
	Contact? picked;
	PhoneSession? session;
	TrayIcon? tray;
	bool loading = true;
	bool quitting;

	/// <param name="demoScene">When set, shows invented data instead of the real PC and phone: pairing, idle, ringing or call.</param>
	public MainWindow(string? demoScene = null)
	{
		InitializeComponent();
		demo = demoScene != null;
		settings = demo ? new AppSettings { Persist = false, PairingCode = "0000000000000000" } : AppSettings.Load();
		if (settings.PairingCode == null)
		{
			settings.PairingCode = CallBridgeServer.GenerateCode();
			settings.Save();
		}
		server = new CallBridgeServer(settings.PairingCode);
		ShowQr();
		FillDevices(MicCombo, demo ? DemoDevices.Microphones : AudioDevices.Capture(), settings.MicId);
		FillDevices(SpeakerCombo, demo ? DemoDevices.Outputs : AudioDevices.Render(), settings.SpeakerId);
		VolumeSlider.Value = settings.Volume;
		VolumeText.Text = $"{settings.Volume * 100:0}%";
		loading = false;

		if (demo)
		{
			quitting = true;
			ShowDemo(demoScene!);
			Ready = true;
			return;
		}

		server.PhoneConnected += s => Dispatcher.Invoke(() => OnPhoneConnected(s));
		server.PhoneDisconnected += () => Dispatcher.Invoke(OnPhoneDisconnected);
		try
		{
			server.Start();
		}
		catch (SocketException e)
		{
			MessageBox.Show($"Port {server.Port} is busy: {e.Message}", "CallBridge", MessageBoxButton.OK, MessageBoxImage.Error);
			return;
		}

		tray = new TrayIcon(ShowFromTray, Quit);
		levelTimer.Tick += (_, _) => MicLevel.Value = session?.MicLevel ?? 0;
		levelTimer.Start();
		Closing += OnClosing;
		Closed += (_, _) =>
		{
			tray?.Dispose();
			server.Dispose();
		};
		Ready = true;
	}

	public bool Ready { get; }

	public void ShowFromTray()
	{
		Show();
		if (WindowState == WindowState.Minimized)
			WindowState = WindowState.Normal;
		Activate();
	}

	void Quit()
	{
		quitting = true;
		Application.Current.Shutdown();
	}

	void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
	{
		if (quitting)
			return;
		e.Cancel = true;
		Hide();
		tray?.Notify("CallBridge", "Still running in the tray, calls will be shown here");
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
		var hosts = demo ? ["192.168.1.10"] : new[] { "127.0.0.1" }.Concat(PairingInfo.LocalAddresses());
		var uri = PairingInfo.BuildUri(settings.PairingCode!, CallBridgeServer.DefaultPort, hosts, demo ? "My PC" : Environment.MachineName);
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
		s.ContactsChanged += list => Dispatcher.Invoke(() => contacts = list);
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
		if (ringing && (!IsVisible || !IsActive))
			tray?.Notify("Incoming call", who);
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
		var text = DialBox.Text.Trim();
		if (text.Length == 0)
			return;
		var number = ResolveNumber(text);
		if (number == null)
		{
			ShowDialHint("No such contact. Type a number or pick a contact from the list");
			return;
		}
		SuggestionPopup.IsOpen = false;
		_ = session?.SendCommandAsync("dial", new { Number = number });
	}

	static bool LooksLikeNumber(string text) => text.All(c => char.IsDigit(c) || "+*#-() ".Contains(c));

	static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());

	string? ResolveNumber(string text)
	{
		if (picked != null && text == picked.Name)
			return picked.Number;
		if (LooksLikeNumber(text))
			return text;
		if (SuggestionList.SelectedItem is Contact suggested)
		{
			Pick(suggested);
			return suggested.Number;
		}
		return null;
	}

	void Pick(Contact contact)
	{
		picked = contact;
		SuggestionPopup.IsOpen = false;
		DialBox.Text = contact.Name;
		DialBox.CaretIndex = DialBox.Text.Length;
	}

	void ShowDialHint(string? message)
	{
		DialHint.Text = message ?? "";
		DialHint.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
	}

	void OnDialTextChanged(object sender, TextChangedEventArgs e)
	{
		ShowDialHint(null);
		var text = DialBox.Text.Trim();
		if (picked != null && text != picked.Name)
			picked = null;
		if (picked != null || text.Length == 0 || !DialBox.IsKeyboardFocusWithin)
		{
			SuggestionPopup.IsOpen = false;
			return;
		}
		var digits = Digits(text);
		var matches = contacts
			.Where(c => c.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) || (digits.Length >= 2 && Digits(c.Number).Contains(digits)))
			.Take(6)
			.ToList();
		SuggestionList.ItemsSource = matches;
		SuggestionList.SelectedIndex = matches.Count > 0 ? 0 : -1;
		SuggestionPopup.IsOpen = matches.Count > 0;
	}

	void OnDialKey(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Down or Key.Up when SuggestionPopup.IsOpen:
				var count = SuggestionList.Items.Count;
				SuggestionList.SelectedIndex = (SuggestionList.SelectedIndex + (e.Key == Key.Down ? 1 : count - 1)) % count;
				e.Handled = true;
				break;
			case Key.Escape:
				SuggestionPopup.IsOpen = false;
				break;
			case Key.Enter when DialButton.IsEnabled:
				OnDial(sender, e);
				e.Handled = true;
				break;
		}
	}

	void OnDialLostFocus(object sender, KeyboardFocusChangedEventArgs e) => SuggestionPopup.IsOpen = false;

	void OnSuggestionClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is ListBoxItem { DataContext: Contact contact })
		{
			e.Handled = true;
			Pick(contact);
			DialBox.Focus();
		}
	}
}
