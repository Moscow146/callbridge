using System.Net.Sockets;
using System.Text.Json;
using CallBridge.Core.Audio;
using CallBridge.Core.Protocol;

namespace CallBridge.Core.Session;

/// <summary>One connected phone: routes call audio and control messages.</summary>
public sealed class PhoneSession : IDisposable
{
	readonly TcpClient client;
	readonly FrameStream frames;
	readonly VoiceDecoder decoder = new();
	readonly VoiceEncoder encoder = new();
	readonly CancellationTokenSource cts = new();
	SpeakerPlayer? speaker;
	MicCapture? mic;

	public string DeviceName { get; private set; } = "";

	public bool Muted { get; set; }

	public bool Deafened { get; set; }

	public event Action<PhoneState>? StateChanged;

	public event Action<string?>? AudioRouteChanged;

	public event Action<IReadOnlyList<Contact>>? ContactsChanged;

	public event Action? Closed;

	public PhoneSession(TcpClient client)
	{
		this.client = client;
		client.NoDelay = true;
		frames = new FrameStream(client.GetStream());
	}

	/// <summary>Performs the handshake. Returns false when the phone is rejected.</summary>
	public async Task<bool> HandshakeAsync(string expectedToken, CancellationToken ct)
	{
		var first = await frames.ReadAsync(ct);
		if (first is not { Type: FrameType.Hello } hello)
			return false;
		var parsed = Json.Parse<Hello>(hello.Payload);
		var ok = parsed != null && parsed.Version == Json.ProtocolVersion && parsed.Token == expectedToken;
		var error = parsed == null ? "bad hello" : parsed.Version != Json.ProtocolVersion ? "version mismatch" : !ok ? "wrong code" : null;
		await frames.WriteAsync(FrameType.HelloAck, Json.Bytes(new HelloAck(Json.ProtocolVersion, ok, error)), ct);
		if (!ok)
			return false;
		DeviceName = parsed!.Device;
		return true;
	}

	public void SetDevices(string? micId, string? speakerId, float volume)
	{
		var oldSpeaker = speaker;
		var oldMic = mic;
		speaker = new SpeakerPlayer(speakerId) { Volume = volume };
		speaker.Start();
		mic = new MicCapture(micId);
		mic.FrameReady += OnMicFrame;
		mic.Start();
		oldSpeaker?.Dispose();
		oldMic?.Dispose();
	}

	public float MicLevel => mic?.Level ?? 0f;

	public float SpeakerVolume
	{
		set
		{
			if (speaker != null)
				speaker.Volume = value;
		}
	}

	void OnMicFrame(short[] pcm)
	{
		if (Muted)
			return;
		var packet = encoder.Encode(pcm);
		_ = frames.WriteAsync(FrameType.Uplink, packet, cts.Token).ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
	}

	public Task SendCommandAsync(string type, object? extra = null) =>
		frames.WriteAsync(FrameType.Control, Json.Command(type, extra), cts.Token);

	public async Task RunAsync()
	{
		try
		{
			while (!cts.IsCancellationRequested)
			{
				var frame = await frames.ReadAsync(cts.Token);
				if (frame == null)
					break;
				Handle(frame.Value);
			}
		}
		catch (Exception e) when (e is IOException or OperationCanceledException or EndOfStreamException or InvalidDataException or ObjectDisposedException)
		{
		}
		finally
		{
			Closed?.Invoke();
		}
	}

	void Handle(Frame frame)
	{
		switch (frame.Type)
		{
			case FrameType.Downlink:
				if (!Deafened)
					speaker?.Enqueue(decoder.Decode(frame.Payload));
				break;
			case FrameType.Ping:
				_ = frames.WriteAsync(FrameType.Pong, frame.Payload, cts.Token);
				break;
			case FrameType.Control:
				HandleControl(frame.Payload);
				break;
		}
	}

	void HandleControl(byte[] payload)
	{
		using var doc = JsonDocument.Parse(payload);
		var root = doc.RootElement;
		switch (Json.TypeOf(root))
		{
			case "state":
				var state = root.Deserialize<PhoneState>(Json.Options);
				if (state != null)
					StateChanged?.Invoke(state);
				break;
			case "contacts":
				var items = root.TryGetProperty("items", out var array) ? array.Deserialize<List<Contact>>(Json.Options) : null;
				if (items != null)
					ContactsChanged?.Invoke(items);
				break;
			case "audio":
				var ok = root.TryGetProperty("route", out var r) && r.GetString() == "ok";
				AudioRouteChanged?.Invoke(ok ? null : root.TryGetProperty("reason", out var reason) ? reason.GetString() : "unavailable");
				break;
		}
	}

	public void Dispose()
	{
		cts.Cancel();
		mic?.Dispose();
		speaker?.Dispose();
		client.Dispose();
	}
}
