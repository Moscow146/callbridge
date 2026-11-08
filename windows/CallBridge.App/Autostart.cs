using System.IO;
using CallBridge.Core.Startup;
using Microsoft.Win32;

namespace CallBridge.App;

static class Autostart
{
	const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
	const string ValueName = "CallBridge";
	const string ExecutableName = "CallBridge.exe";

	public static bool Supported => string.Equals(Path.GetFileName(Environment.ProcessPath), ExecutableName, StringComparison.OrdinalIgnoreCase);

	public static bool Enabled
	{
		get
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(RunKey);
				return key?.GetValue(ValueName) is string;
			}
			catch (Exception e) when (e is System.Security.SecurityException or IOException or UnauthorizedAccessException)
			{
				return false;
			}
		}
	}

	public static void Set(bool enabled)
	{
		using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey);
		if (enabled)
			key.SetValue(ValueName, AutostartCommand.Build(Environment.ProcessPath!), RegistryValueKind.String);
		else
			key.DeleteValue(ValueName, false);
	}
}
