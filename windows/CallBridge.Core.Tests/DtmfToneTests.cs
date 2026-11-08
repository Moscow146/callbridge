using CallBridge.Core.Audio;

namespace CallBridge.Core.Tests;

public class DtmfToneTests
{
	static double Magnitude(float[] samples, double frequency, int rate)
	{
		double re = 0, im = 0;
		for (var i = 0; i < samples.Length; i++)
		{
			var angle = 2 * Math.PI * frequency * i / rate;
			re += samples[i] * Math.Cos(angle);
			im += samples[i] * Math.Sin(angle);
		}
		return Math.Sqrt(re * re + im * im) / samples.Length;
	}

	[Theory]
	[InlineData('1', 697, 1209)]
	[InlineData('5', 770, 1336)]
	[InlineData('9', 852, 1477)]
	[InlineData('*', 941, 1209)]
	[InlineData('0', 941, 1336)]
	[InlineData('#', 941, 1477)]
	public void ContainsBothFrequencies(char digit, double low, double high)
	{
		var samples = DtmfTone.Generate(digit);
		var otherLow = low == 697 ? 770 : 697;
		var otherHigh = high == 1209 ? 1336 : 1209;
		Assert.True(Magnitude(samples, low, VoiceFormat.SampleRate) > 0.03);
		Assert.True(Magnitude(samples, high, VoiceFormat.SampleRate) > 0.03);
		Assert.True(Magnitude(samples, otherLow, VoiceFormat.SampleRate) < 0.01);
		Assert.True(Magnitude(samples, otherHigh, VoiceFormat.SampleRate) < 0.01);
	}

	[Fact]
	public void DurationIsShort()
	{
		var samples = DtmfTone.Generate('3');
		var ms = samples.Length * 1000.0 / VoiceFormat.SampleRate;
		Assert.InRange(ms, 120, 160);
	}

	[Fact]
	public void NeverClipsAndStaysQuiet()
	{
		foreach (var digit in "0123456789*#")
		{
			var samples = DtmfTone.Generate(digit);
			Assert.All(samples, s => Assert.InRange(Math.Abs(s), 0f, 0.4f));
		}
	}

	[Fact]
	public void FadesInAndOutWithoutClicks()
	{
		var samples = DtmfTone.Generate('7');
		Assert.Equal(0f, samples[0]);
		Assert.True(Math.Abs(samples[^1]) < 0.001f);
		for (var i = 1; i < samples.Length; i++)
			Assert.True(Math.Abs(samples[i] - samples[i - 1]) < 0.1f);
	}

	[Fact]
	public void UnknownKeyIsSilent() => Assert.Empty(DtmfTone.Generate('x'));

	[Fact]
	public void AllSixteenKeysHaveFrequencies()
	{
		foreach (var key in "123A456B789C*0#D")
			Assert.True(DtmfTone.TryGetFrequencies(key, out _, out _));
	}
}
