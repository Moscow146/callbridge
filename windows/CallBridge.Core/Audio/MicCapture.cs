using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace CallBridge.Core.Audio;

/// <summary>Captures a microphone as 48 kHz mono and raises fixed 20 ms frames.</summary>
public sealed class MicCapture : IDisposable
{
	readonly WasapiCapture capture;
	readonly MMDevice device;
	readonly short[] frame = new short[VoiceFormat.FrameSamples];
	int filled;

	public event Action<short[]>? FrameReady;

	int peakBits;

	public float Level { get; private set; }

	public float TakePeak() => BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref peakBits, 0));

	public MicCapture(string? deviceId)
	{
		device = AudioDevices.Open(DataFlow.Capture, deviceId);
		capture = new WasapiCapture(device, true, 20) { WaveFormat = VoiceFormat.Pcm };
		capture.DataAvailable += OnData;
	}

	public void Start() => capture.StartRecording();

	void OnData(object? sender, WaveInEventArgs e)
	{
		var samples = e.BytesRecorded / 2;
		for (var i = 0; i < samples; i++)
		{
			var s = BitConverter.ToInt16(e.Buffer, i * 2);
			frame[filled++] = s;
			if (filled == frame.Length)
			{
				filled = 0;
				FrameReady?.Invoke((short[])frame.Clone());
			}
		}
		var peak = LevelMeter.Peak(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(e.Buffer.AsSpan(0, samples * 2)));
		Level = peak;
		int seen;
		do
		{
			seen = Volatile.Read(ref peakBits);
			if (BitConverter.Int32BitsToSingle(seen) >= peak)
				break;
		}
		while (Interlocked.CompareExchange(ref peakBits, BitConverter.SingleToInt32Bits(peak), seen) != seen);
	}

	public void Dispose()
	{
		capture.DataAvailable -= OnData;
		try
		{
			capture.StopRecording();
		}
		catch (InvalidOperationException)
		{
		}
		capture.Dispose();
		device.Dispose();
	}
}
