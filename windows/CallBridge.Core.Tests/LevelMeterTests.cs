using CallBridge.Core.Audio;

namespace CallBridge.Core.Tests;

public class LevelMeterTests
{
	[Fact]
	public void SilenceIsZero()
	{
		var samples = new short[960];
		Assert.Equal(0f, LevelMeter.Peak(samples));
		Assert.Equal(0f, LevelMeter.Rms(samples));
		Assert.Equal(0, LevelMeter.ToMeter(0f));
	}

	[Fact]
	public void PeakHandlesMinValueAndFullScale()
	{
		Assert.Equal(1f, LevelMeter.Peak(new short[] { 0, short.MinValue, 5 }));
		Assert.Equal(0.5f, LevelMeter.Peak(new short[] { 16384, -100 }));
	}

	[Fact]
	public void RmsOfSquareWaveIsItsAmplitude()
	{
		var samples = new short[] { 16384, -16384, 16384, -16384 };
		Assert.Equal(0.5f, LevelMeter.Rms(samples), 4);
	}

	[Fact]
	public void MeterFollowsDecibels()
	{
		Assert.Equal(1, LevelMeter.ToMeter(1f));
		Assert.Equal(0.5, LevelMeter.ToMeter(0.0316228f), 3);
		Assert.Equal(0, LevelMeter.ToMeter(0.0001f));
	}

	[Fact]
	public void GainRaisesTheMeterAndIsClamped()
	{
		var quiet = LevelMeter.ToMeter(0.01f, 1f);
		var boosted = LevelMeter.ToMeter(0.01f, 3f);
		Assert.True(boosted > quiet);
		Assert.Equal(1, LevelMeter.ToMeter(0.9f, 3f));
		Assert.Equal(0, LevelMeter.ToMeter(0.5f, 0f));
	}

	[Fact]
	public void SmootherRisesFasterThanItFalls()
	{
		var rise = new LevelSmoother();
		var up = rise.Update(1, 0.016);
		var fall = new LevelSmoother();
		for (var i = 0; i < 100; i++)
			fall.Update(1, 0.016);
		var before = fall.Value;
		var down = before - fall.Update(0, 0.016);
		Assert.True(up > down);
		Assert.True(before > 0.99);
	}

	[Fact]
	public void SmootherSettlesToTargetAndToZero()
	{
		var smoother = new LevelSmoother();
		for (var i = 0; i < 200; i++)
			smoother.Update(0.6, 0.016);
		Assert.Equal(0.6, smoother.Value, 3);
		for (var i = 0; i < 400; i++)
			smoother.Update(0, 0.016);
		Assert.Equal(0, smoother.Value);
	}

	[Fact]
	public void SmootherIgnoresNonPositiveElapsedTime()
	{
		var smoother = new LevelSmoother();
		Assert.Equal(0, smoother.Update(1, 0));
		Assert.Equal(0, smoother.Update(1, -1));
	}
}
