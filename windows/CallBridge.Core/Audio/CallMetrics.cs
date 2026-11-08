namespace CallBridge.Core.Audio;

public sealed record CallMetricsSnapshot(
	double RoundTripMs,
	double RoundTripMaxMs,
	int RoundTripSamples,
	double JitterMs,
	double MaxGapMs,
	int LateFrames,
	int FramesReceived,
	double BufferedMs,
	int BufferTrims)
{
	public const double PlaybackCapMs = 160;
	public const double LateGapMs = 60;

	public double EstimatedDelayMs => RoundTripSamples == 0 ? BufferedMs : RoundTripMs / 2 + BufferedMs;

	public bool OverDelayCap => BufferedMs > PlaybackCapMs;

	public string Summary() => RoundTripSamples == 0
		? $"buffer {BufferedMs:0} ms, jitter {JitterMs:0.0} ms, late {LateFrames}, trims {BufferTrims}"
		: $"delay ~{EstimatedDelayMs:0} ms, rtt {RoundTripMs:0} ms (max {RoundTripMaxMs:0}), jitter {JitterMs:0.0} ms, late {LateFrames}, trims {BufferTrims}";
}

public sealed class CallMetrics
{
	readonly object gate = new();
	double lastArrivalMs = double.NaN;
	double jitter;
	double maxGap;
	int late;
	int frames;
	double rtt;
	double rttMax;
	int rttSamples;

	public void FrameArrived(double nowMs)
	{
		lock (gate)
		{
			frames++;
			if (!double.IsNaN(lastArrivalMs))
			{
				var gap = nowMs - lastArrivalMs;
				jitter += (Math.Abs(gap - VoiceFormat.FrameMs) - jitter) / 16;
				maxGap = Math.Max(maxGap, gap);
				if (gap >= CallMetricsSnapshot.LateGapMs)
					late++;
			}
			lastArrivalMs = nowMs;
		}
	}

	public void RoundTrip(double ms)
	{
		if (ms < 0)
			return;
		lock (gate)
		{
			rtt = rttSamples == 0 ? ms : rtt + (ms - rtt) / 4;
			rttMax = Math.Max(rttMax, ms);
			rttSamples++;
		}
	}

	public CallMetricsSnapshot Snapshot(double bufferedMs, int bufferTrims)
	{
		lock (gate)
			return new CallMetricsSnapshot(rtt, rttMax, rttSamples, jitter, maxGap, late, frames, bufferedMs, bufferTrims);
	}
}
