using CallBridge.Core.Audio;

namespace CallBridge.Core.Tests;

public class VoiceCodecTests
{
	[Fact]
	public void SineSurvivesRoundTrip()
	{
		var encoder = new VoiceEncoder();
		var decoder = new VoiceDecoder();
		var pcm = new short[VoiceFormat.FrameSamples];
		short[] decoded = [];
		for (var frame = 0; frame < 10; frame++)
		{
			for (var i = 0; i < pcm.Length; i++)
			{
				var t = (frame * pcm.Length + i) / (double)VoiceFormat.SampleRate;
				pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * t) * 12000);
			}
			var packet = encoder.Encode(pcm);
			Assert.InRange(packet.Length, 1, 200);
			decoded = decoder.Decode(packet);
		}
		Assert.Equal(VoiceFormat.FrameSamples, decoded.Length);
		Assert.True(decoded.Max(s => Math.Abs((int)s)) > 4000);
	}
}
