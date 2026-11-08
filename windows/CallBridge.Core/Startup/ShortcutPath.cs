namespace CallBridge.Core.Startup;

public static class ShortcutPath
{
	public const string Extension = ".lnk";

	public static string In(string directory, string name) => Path.Combine(directory, name + Extension);
}
