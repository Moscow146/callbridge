using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace CallBridge.Core.Audio;

public sealed class DtmfTonePlayer
{
	public void Play(string? deviceId, char digit)
	{
		var samples = DtmfTone.Generate(digit);
		if (samples.Length == 0)
			return;
		_ = Task.Run(() => Run(deviceId, samples));
	}

	static void Run(string? deviceId, float[] samples)
	{
		try
		{
			using var device = AudioDevices.Open(DataFlow.Render, deviceId);
			var padded = new float[samples.Length + VoiceFormat.SampleRate / 10];
			samples.CopyTo(padded, 0);
			var bytes = new byte[padded.Length * 4];
			Buffer.BlockCopy(padded, 0, bytes, 0, bytes.Length);
			using var source = new RawSourceWaveStream(new MemoryStream(bytes), WaveFormat.CreateIeeeFloatWaveFormat(VoiceFormat.SampleRate, 1));
			using var output = new WasapiOut(device, AudioClientShareMode.Shared, true, 40);
			using var done = new ManualResetEventSlim();
			output.PlaybackStopped += (_, _) => done.Set();
			output.Init(source);
			output.Play();
			done.Wait(1500);
			output.Stop();
		}
		catch (Exception)
		{
		}
	}
}
