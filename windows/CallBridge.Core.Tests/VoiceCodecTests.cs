using CallBridge.Core.Audio;
using Concentus;
using Concentus.Enums;

namespace CallBridge.Core.Tests;

public class VoiceCodecTests
{
	[Fact]
	public void DecodesWhatThePhoneSends()
	{
		var encoder = OpusCodecFactory.CreateEncoder(VoiceFormat.SampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
		encoder.Bitrate = 32000;
		var decoder = new VoiceDecoder();
		var pcm = new short[VoiceFormat.FrameSamples];
		var packet = new byte[1275];
		short[] decoded = [];
		for (var frame = 0; frame < 10; frame++)
		{
			for (var i = 0; i < pcm.Length; i++)
			{
				var t = (frame * pcm.Length + i) / (double)VoiceFormat.SampleRate;
				pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * t) * 12000);
			}
			var length = encoder.Encode(pcm, VoiceFormat.FrameSamples, packet, packet.Length);
			Assert.InRange(length, 1, 200);
			decoded = decoder.Decode(packet.AsSpan(0, length));
		}
		Assert.Equal(VoiceFormat.FrameSamples, decoded.Length);
		Assert.True(decoded.Max(s => Math.Abs((int)s)) > 4000);
	}
}
