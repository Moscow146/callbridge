using System.Windows;

namespace CallBridge.App;

public partial class App : Application
{
	const string SingleInstanceName = "CallBridge.SingleInstance";
	const string ShowEventName = "CallBridge.Show";

	Mutex? singleInstance;
	EventWaitHandle? showEvent;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		singleInstance = new Mutex(true, SingleInstanceName, out var first);
		if (!first)
		{
			using var existing = EventWaitHandle.OpenExisting(ShowEventName);
			existing.Set();
			Shutdown();
			return;
		}

		showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
		var window = new MainWindow();
		if (!window.Ready)
		{
			Shutdown();
			return;
		}
		MainWindow = window;
		var waiter = new Thread(() =>
		{
			while (showEvent.WaitOne())
				Dispatcher.Invoke(window.ShowFromTray);
		}) { IsBackground = true };
		waiter.Start();
		window.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		singleInstance?.ReleaseMutex();
		singleInstance?.Dispose();
		base.OnExit(e);
	}
}
