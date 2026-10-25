using System.IO;
using System.Text.Json;

namespace CallBridge.App;

public sealed class AppSettings
{
	static readonly string Path = System.IO.Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CallBridge", "settings.json");

	public string? PairingCode { get; set; }

	public string? MicId { get; set; }

	public string? SpeakerId { get; set; }

	public double Volume { get; set; } = 1;

	public static AppSettings Load()
	{
		try
		{
			return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path)) ?? new AppSettings();
		}
		catch (Exception e) when (e is IOException or JsonException)
		{
			return new AppSettings();
		}
	}

	public void Save()
	{
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
		File.WriteAllText(Path, JsonSerializer.Serialize(this));
	}
}
