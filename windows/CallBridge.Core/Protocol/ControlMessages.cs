using System.Text.Json;
using System.Text.Json.Serialization;

namespace CallBridge.Core.Protocol;

public enum CallState
{
	Idle,
	Ringing,
	Dialing,
	Active,
	Held,
}

public sealed record Hello(int Version, string Device, string Token);

public sealed record HelloAck(int Version, bool Ok, string? Error = null);

public sealed record PhoneState(CallState Call, string? Number, string? Name);

public sealed record Contact(string Name, string Number);

public static class Json
{
	public const int ProtocolVersion = 1;

	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public static byte[] Bytes<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

	public static T? Parse<T>(byte[] payload) => JsonSerializer.Deserialize<T>(payload, Options);

	public static byte[] Command(string type, object? extra = null)
	{
		var dict = new Dictionary<string, object?> { ["t"] = type };
		if (extra != null)
		{
			foreach (var p in extra.GetType().GetProperties())
				dict[JsonNamingPolicy.CamelCase.ConvertName(p.Name)] = p.GetValue(extra);
		}
		return Bytes(dict);
	}

	/// <summary>Reads the "t" discriminator of a control message.</summary>
	public static string? TypeOf(JsonElement e) => e.TryGetProperty("t", out var t) ? t.GetString() : null;
}
