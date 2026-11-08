using CallBridge.Core.Audio;

namespace CallBridge.Core.Tests;

public class EchoCancellerTests
{
	const int Frame = VoiceFormat.FrameSamples;

	static short[] Noise(Random random, int amplitude)
	{
		var frame = new short[Frame];
		for (var i = 0; i < frame.Length; i++)
			frame[i] = (short)(random.Next(-amplitude, amplitude));
		return frame;
	}

	static double Energy(short[] frame) => frame.Sum(s => (double)s * s) / frame.Length;

	[Fact]
	public void PassesNearEndUntouchedWithoutFarEnd()
	{
		var canceller = new EchoCanceller();
		var random = new Random(1);
		for (var f = 0; f < 20; f++)
		{
			var mic = Noise(random, 8000);
			var expected = (short[])mic.Clone();
			canceller.Process(mic);
			Assert.Equal(expected, mic);
		}
	}

	[Fact]
	public void RemovesDelayedEchoOfTheFarEnd()
	{
		var canceller = new EchoCanceller(taps: 2400, delayMs: 60);
		var random = new Random(2);
		var echoDelay = VoiceFormat.SampleRate * 70 / 1000;
		var farAll = new List<short>();
		var micAll = new List<short>();
		var gains = new (int Offset, float Gain)[] { (0, 0.5f), (30, -0.25f), (180, 0.12f) };

		double before = 0, after = 0;
		for (var f = 0; f < 400; f++)
		{
			var far = Noise(random, 6000);
			farAll.AddRange(far);
			var mic = new short[Frame];
			for (var i = 0; i < Frame; i++)
			{
				var n = farAll.Count - Frame + i;
				double v = 0;
				foreach (var (offset, gain) in gains)
				{
					var idx = n - echoDelay - offset;
					if (idx >= 0)
						v += farAll[idx] * gain;
				}
				mic[i] = (short)v;
			}
			micAll.AddRange(mic);
			canceller.AddFarEnd(far);
			var raw = Energy(mic);
			canceller.Process(mic);
			if (f >= 380)
			{
				before += raw;
				after += Energy(mic);
			}
		}
		var reductionDb = 10 * Math.Log10(before / after);
		Assert.True(reductionDb > 15, $"reduction {reductionDb:0.0} dB");
		Assert.True(canceller.ReductionDb > 10);
	}

	[Fact]
	public void KeepsNearEndSpeechWhileEchoIsRemoved()
	{
		var canceller = new EchoCanceller(taps: 2400, delayMs: 60);
		var random = new Random(3);
		var echoDelay = VoiceFormat.SampleRate * 70 / 1000;
		var farAll = new List<short>();
		double nearEnergy = 0, outEnergy = 0;
		for (var f = 0; f < 500; f++)
		{
			var far = Noise(random, 5000);
			farAll.AddRange(far);
			var near = new short[Frame];
			if (f >= 450)
			{
				for (var i = 0; i < Frame; i++)
					near[i] = (short)(Math.Sin(2 * Math.PI * 300 * (f * Frame + i) / VoiceFormat.SampleRate) * 12000);
			}
			var mic = new short[Frame];
			for (var i = 0; i < Frame; i++)
			{
				var idx = farAll.Count - Frame + i - echoDelay;
				mic[i] = (short)((idx >= 0 ? farAll[idx] * 0.4f : 0) + near[i]);
			}
			canceller.AddFarEnd(far);
			canceller.Process(mic);
			if (f >= 460)
			{
				nearEnergy += Energy(near);
				outEnergy += Energy(mic);
			}
		}
		Assert.InRange(outEnergy / nearEnergy, 0.6, 1.6);
	}

	[Fact]
	public void IgnoresAbsurdFarEndBacklog()
	{
		var canceller = new EchoCanceller();
		var random = new Random(4);
		for (var i = 0; i < 100; i++)
			canceller.AddFarEnd(Noise(random, 1000));
		var mic = Noise(random, 1000);
		canceller.Process(mic);
		Assert.Equal(Frame, mic.Length);
	}
}
