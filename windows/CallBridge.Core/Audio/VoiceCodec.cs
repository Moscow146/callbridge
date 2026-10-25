using Concentus;

namespace CallBridge.Core.Audio;

public sealed class VoiceDecoder
{
	readonly IOpusDecoder decoder = OpusCodecFactory.CreateDecoder(VoiceFormat.SampleRate, 1);

	public short[] Decode(ReadOnlySpan<byte> packet)
	{
		var pcm = new short[VoiceFormat.FrameSamples];
		var n = decoder.Decode(packet, pcm, VoiceFormat.FrameSamples, false);
		return n == pcm.Length ? pcm : pcm.AsSpan(0, n).ToArray();
	}

	public short[] Conceal()
	{
		var pcm = new short[VoiceFormat.FrameSamples];
		decoder.Decode(ReadOnlySpan<byte>.Empty, pcm, VoiceFormat.FrameSamples, false);
		return pcm;
	}
}
