namespace CallBridge.Core.Audio;

public static class DtmfTone
{
	public const int DurationMs = 140;
	public const int RampMs = 12;
	public const float Level = 0.18f;

	static readonly double[] Rows = [697, 770, 852, 941];
	static readonly double[] Columns = [1209, 1336, 1477, 1633];
	const string Layout = "123A456B789C*0#D";

	public static bool TryGetFrequencies(char digit, out double low, out double high)
	{
		var index = Layout.IndexOf(char.ToUpperInvariant(digit));
		low = index < 0 ? 0 : Rows[index / 4];
		high = index < 0 ? 0 : Columns[index % 4];
		return index >= 0;
	}

	public static float[] Generate(char digit, int sampleRate = VoiceFormat.SampleRate)
	{
		if (!TryGetFrequencies(digit, out var low, out var high))
			return [];
		var count = sampleRate * DurationMs / 1000;
		var ramp = sampleRate * RampMs / 1000;
		var samples = new float[count];
		for (var i = 0; i < count; i++)
		{
			var t = (double)i / sampleRate;
			var wave = 0.5 * (Math.Sin(2 * Math.PI * low * t) + Math.Sin(2 * Math.PI * high * t));
			var edge = Math.Min(i, count - 1 - i);
			var gain = edge >= ramp ? 1.0 : 0.5 - 0.5 * Math.Cos(Math.PI * edge / ramp);
			samples[i] = (float)(wave * gain * Level);
		}
		return samples;
	}
}
