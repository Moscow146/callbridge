using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CallBridge.Core.Audio;

/// <summary>Plays 48 kHz mono PCM frames to a chosen output device.</summary>
public sealed class SpeakerPlayer : IDisposable
{
	static readonly TimeSpan MaxLatency = TimeSpan.FromMilliseconds(160);

	readonly BufferedWaveProvider buffer;
	readonly VolumeSampleProvider volume;
	readonly WasapiOut output;
	readonly MMDevice device;

	public float Volume
	{
		get => volume.Volume;
		set => volume.Volume = Math.Clamp(value, 0f, 2f);
	}

	public SpeakerPlayer(string? deviceId)
	{
		device = AudioDevices.Open(DataFlow.Render, deviceId);
		buffer = new BufferedWaveProvider(VoiceFormat.Pcm)
		{
			BufferDuration = TimeSpan.FromMilliseconds(400),
			DiscardOnBufferOverflow = true,
		};
		volume = new VolumeSampleProvider(buffer.ToSampleProvider());
		output = new WasapiOut(device, AudioClientShareMode.Shared, true, 40);
		output.Init(volume.ToWaveProvider16());
	}

	public void Start() => output.Play();

	public void Enqueue(short[] pcm)
	{
		if (buffer.BufferedDuration > MaxLatency)
			buffer.ClearBuffer();
		var bytes = new byte[pcm.Length * 2];
		Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
		buffer.AddSamples(bytes, 0, bytes.Length);
	}

	public void Dispose()
	{
		output.Stop();
		output.Dispose();
		device.Dispose();
	}
}
