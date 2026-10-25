using Concentus;
using Concentus.Enums;

namespace CallBridge.Core.Audio;

public sealed class VoiceEncoder
{
	readonly IOpusEncoder encoder;
	readonly byte[] buffer = new byte[1275];

	public VoiceEncoder()
	{
		encoder = OpusCodecFactory.CreateEncoder(VoiceFormat.SampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
		encoder.Bitrate = VoiceFormat.Bitrate;
		encoder.UseInbandFEC = true;
		encoder.PacketLossPercent = 5;
	}

	public byte[] Encode(ReadOnlySpan<short> pcm)
	{
		var n = encoder.Encode(pcm, VoiceFormat.FrameSamples, buffer, buffer.Length);
		return buffer.AsSpan(0, n).ToArray();
	}
}

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
