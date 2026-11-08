namespace CallBridge.Core.Audio;

public static class LevelMeter
{
	public const double FloorDb = -60;

	public static float Peak(ReadOnlySpan<short> samples)
	{
		var peak = 0;
		foreach (var s in samples)
			peak = Math.Max(peak, Math.Abs((int)s));
		return Math.Min(1f, peak / 32768f);
	}

	public static float Rms(ReadOnlySpan<short> samples)
	{
		if (samples.IsEmpty)
			return 0;
		double sum = 0;
		foreach (var s in samples)
			sum += (double)s * s;
		return (float)(Math.Sqrt(sum / samples.Length) / 32768.0);
	}

	public static double ToMeter(float level, float gain = 1f)
	{
		var amplified = level * Math.Max(0f, gain);
		if (!(amplified > 0))
			return 0;
		return Math.Clamp(1 + 20 * Math.Log10(amplified) / -FloorDb, 0, 1);
	}
}

public sealed class LevelSmoother
{
	readonly double attack;
	readonly double release;

	public LevelSmoother(double attackSeconds = 0.04, double releaseSeconds = 0.28)
	{
		attack = attackSeconds;
		release = releaseSeconds;
	}

	public double Value { get; private set; }

	public double Update(double target, double elapsedSeconds)
	{
		if (elapsedSeconds <= 0)
			return Value;
		var time = target > Value ? attack : release;
		var step = time <= 0 ? 1 : 1 - Math.Exp(-elapsedSeconds / time);
		Value += (target - Value) * step;
		if (Value < 0.0005)
			Value = 0;
		return Value;
	}

	public void Reset() => Value = 0;
}
