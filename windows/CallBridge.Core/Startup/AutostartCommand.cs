namespace CallBridge.Core.Startup;

public static class AutostartCommand
{
	public const string MinimizedArgument = "--minimized";

	public static string Build(string executablePath) => $"\"{executablePath}\" {MinimizedArgument}";

	public static bool StartsMinimized(IEnumerable<string> args) =>
		args.Any(a => string.Equals(a, MinimizedArgument, StringComparison.OrdinalIgnoreCase));
}
