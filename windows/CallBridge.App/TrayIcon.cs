using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace CallBridge.App;

/// <summary>Notification-area icon: keeps the app reachable while its window is hidden.</summary>
public sealed class TrayIcon : IDisposable
{
	readonly Forms.NotifyIcon icon;

	public TrayIcon(Action open, Action quit)
	{
		var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico")).Stream;
		var menu = new Forms.ContextMenuStrip();
		menu.Items.Add(Strings.Get("TrayOpen"), null, (_, _) => open());
		menu.Items.Add(Strings.Get("TrayQuit"), null, (_, _) => quit());
		icon = new Forms.NotifyIcon
		{
			Icon = new Drawing.Icon(stream),
			Text = "CallBridge",
			ContextMenuStrip = menu,
			Visible = true,
		};
		icon.MouseClick += (_, e) =>
		{
			if (e.Button == Forms.MouseButtons.Left)
				open();
		};
	}

	public void Notify(string title, string text) => icon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.None);

	public void Dispose()
	{
		icon.Visible = false;
		icon.Dispose();
	}
}
