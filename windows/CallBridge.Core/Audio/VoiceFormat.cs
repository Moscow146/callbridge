using NAudio.Wave;

namespace CallBridge.Core.Audio;

/// <summary>Wire audio format shared with the phone.</summary>
public static class VoiceFormat
{
	public const int SampleRate = 48000;
	public const int FrameMs = 20;
	public const int FrameSamples = SampleRate * FrameMs / 1000;
	public const int Bitrate = 32000;

	public static WaveFormat Pcm => new(SampleRate, 16, 1);
}
