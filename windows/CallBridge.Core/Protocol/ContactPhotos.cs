using System.Collections.Concurrent;
using System.Text.Json;

namespace CallBridge.Core.Protocol;

public static class PhotoKey
{
	public const int MinDigits = 4;

	public const int SignificantDigits = 10;

	public static string? Normalize(string? number)
	{
		if (string.IsNullOrEmpty(number))
			return null;
		var digits = new string(number.Where(char.IsAsciiDigit).ToArray());
		if (digits.Length < MinDigits)
			return null;
		return digits.Length > SignificantDigits ? digits[^SignificantDigits..] : digits;
	}

	public static bool TryParseReply(JsonElement root, out string key, out byte[]? data)
	{
		key = "";
		data = null;
		if (!root.TryGetProperty("key", out var k) || k.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(k.GetString()))
			return false;
		key = k.GetString()!;
		if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.String)
		{
			try
			{
				var bytes = Convert.FromBase64String(d.GetString()!);
				data = bytes.Length is > 0 and <= MaxBytes ? bytes : null;
			}
			catch (FormatException)
			{
				data = null;
			}
		}
		return true;
	}

	public const int MaxBytes = 64 * 1024;
}

public sealed class ContactPhotoCache
{
	readonly ConcurrentDictionary<string, byte[]?> memory = new();
	readonly string? directory;
	readonly int maxFiles;
	readonly long maxBytes;

	public ContactPhotoCache(string? directory = null, int maxFiles = 2000, long maxBytes = 32L * 1024 * 1024)
	{
		this.directory = directory;
		this.maxFiles = maxFiles;
		this.maxBytes = maxBytes;
		if (directory != null)
			Prune();
	}

	public bool TryGet(string key, out byte[]? data)
	{
		if (memory.TryGetValue(key, out data))
			return true;
		if (directory != null)
		{
			var path = PathOf(key);
			try
			{
				if (File.Exists(path))
				{
					data = File.ReadAllBytes(path);
					memory[key] = data;
					return true;
				}
			}
			catch (IOException)
			{
			}
		}
		data = null;
		return false;
	}

	public void Put(string key, byte[]? data)
	{
		memory[key] = data;
		if (directory == null)
			return;
		try
		{
			Directory.CreateDirectory(directory);
			var path = PathOf(key);
			if (data == null)
				File.Delete(path);
			else
				File.WriteAllBytes(path, data);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	string PathOf(string key) => Path.Combine(directory!, key + ".jpg");

	void Prune()
	{
		try
		{
			if (!Directory.Exists(directory))
				return;
			var files = new DirectoryInfo(directory).GetFiles("*.jpg").OrderByDescending(f => f.LastWriteTimeUtc).ToList();
			long total = 0;
			for (var i = 0; i < files.Count; i++)
			{
				total += files[i].Length;
				if (i >= maxFiles || total > maxBytes)
					files[i].Delete();
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
