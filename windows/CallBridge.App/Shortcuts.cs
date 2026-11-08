using System.IO;
using System.Runtime.InteropServices;
using CallBridge.Core.Startup;

namespace CallBridge.App;

enum ShortcutPlace
{
	StartMenu,
	Desktop,
}

static class Shortcuts
{
	const string ShortcutName = "CallBridge";

	public static bool Supported => Autostart.Supported;

	static string Folder(ShortcutPlace place) => place == ShortcutPlace.StartMenu
		? Environment.GetFolderPath(Environment.SpecialFolder.Programs)
		: Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

	static string PathFor(ShortcutPlace place) => ShortcutPath.In(Folder(place), ShortcutName);

	public static bool Exists(ShortcutPlace place)
	{
		try
		{
			return File.Exists(PathFor(place));
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
		{
			return false;
		}
	}

	public static void Set(ShortcutPlace place, bool enabled)
	{
		var path = PathFor(place);
		if (!enabled)
		{
			File.Delete(path);
			return;
		}

		var exe = Environment.ProcessPath!;
		Directory.CreateDirectory(Folder(place));
		var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new IOException("Windows Script Host is not available");
		dynamic shell = Activator.CreateInstance(type)!;
		try
		{
			dynamic link = shell.CreateShortcut(path);
			try
			{
				link.TargetPath = exe;
				link.WorkingDirectory = Path.GetDirectoryName(exe);
				link.IconLocation = exe + ",0";
				link.Description = "CallBridge";
				link.Save();
			}
			finally
			{
				Marshal.FinalReleaseComObject(link);
			}
		}
		finally
		{
			Marshal.FinalReleaseComObject(shell);
		}
	}
}
