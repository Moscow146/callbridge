using CallBridge.Core.Audio;

namespace CallBridge.Core.Tests;

public class CallMetricsTests
{
	[Fact]
	public void SteadyFramesGiveNoJitterOrLateFrames()
	{
		var metrics = new CallMetrics();
		for (var i = 0; i < 100; i++)
			metrics.FrameArrived(i * 20);
		var snapshot = metrics.Snapshot(40, 0);
		Assert.Equal(100, snapshot.FramesReceived);
		Assert.Equal(0, snapshot.LateFrames);
		Assert.True(snapshot.JitterMs < 0.01);
		Assert.Equal(20, snapshot.MaxGapMs);
	}

	[Fact]
	public void CountsLateFramesAndTracksJitter()
	{
		var metrics = new CallMetrics();
		var now = 0.0;
		for (var i = 0; i < 50; i++)
		{
			now += i == 25 ? 140 : 20;
			metrics.FrameArrived(now);
		}
		var snapshot = metrics.Snapshot(0, 0);
		Assert.Equal(1, snapshot.LateFrames);
		Assert.Equal(140, snapshot.MaxGapMs);
		Assert.True(snapshot.JitterMs > 0);
	}

	[Fact]
	public void SmoothsRoundTripAndKeepsMaximum()
	{
		var metrics = new CallMetrics();
		metrics.RoundTrip(20);
		metrics.RoundTrip(60);
		metrics.RoundTrip(-5);
		var snapshot = metrics.Snapshot(0, 0);
		Assert.Equal(2, snapshot.RoundTripSamples);
		Assert.Equal(30, snapshot.RoundTripMs, 6);
		Assert.Equal(60, snapshot.RoundTripMaxMs);
	}

	[Fact]
	public void EstimatesDelayFromHalfRoundTripPlusBuffer()
	{
		var metrics = new CallMetrics();
		metrics.RoundTrip(40);
		var snapshot = metrics.Snapshot(50, 2);
		Assert.Equal(70, snapshot.EstimatedDelayMs);
		Assert.False(snapshot.OverDelayCap);
		Assert.True(metrics.Snapshot(200, 0).OverDelayCap);
		Assert.Contains("trims 2", snapshot.Summary());
	}

	[Fact]
	public void LogAppendsLinesAndStaysBounded()
	{
		var path = Path.Combine(Path.GetTempPath(), "cb-metrics-" + Guid.NewGuid() + ".log");
		try
		{
			var log = new CallMetricsLog(path, 200);
			for (var i = 0; i < 20; i++)
				log.Write("line " + i);
			var text = File.ReadAllText(path);
			Assert.Contains("line 19", text);
			Assert.True(text.Length < 400);
		}
		finally
		{
			File.Delete(path);
		}
	}
}
