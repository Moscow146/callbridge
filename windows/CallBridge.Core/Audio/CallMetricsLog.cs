using System.Text;

namespace CallBridge.Core.Audio;

public sealed class CallMetricsLog(string path, long maxBytes = 256 * 1024)
{
	public void Write(string line)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			if (File.Exists(path) && new FileInfo(path).Length > maxBytes)
				File.Delete(path);
			File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}", Encoding.UTF8);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
		}
	}
}
