using System.Numerics;

namespace CallBridge.Core.Audio;

public sealed class EchoCanceller
{
	const float Step = 0.5f;
	const float Regularisation = 1e3f;
	const float DoubleTalkRatio = 0.7f;

	readonly int taps;
	readonly int delaySamples;
	readonly int historyLength;
	readonly float[] history;
	readonly float[] weights;
	readonly Queue<short> pending = new();
	readonly object gate = new();
	int writePos;
	double echoEnergy;
	double residualEnergy;

	public EchoCanceller(int taps = 2400, int delayMs = 60)
	{
		this.taps = (taps + Vector<float>.Count - 1) / Vector<float>.Count * Vector<float>.Count;
		delaySamples = VoiceFormat.SampleRate * delayMs / 1000;
		historyLength = this.taps + delaySamples + VoiceFormat.FrameSamples * 2 + Vector<float>.Count;
		history = new float[historyLength * 2];
		weights = new float[this.taps];
	}

	public double ReductionDb
	{
		get
		{
			lock (gate)
				return echoEnergy < 1e-3 || residualEnergy < 1e-3 ? 0 : 10 * Math.Log10(echoEnergy / residualEnergy);
		}
	}

	public void AddFarEnd(short[] pcm)
	{
		lock (gate)
		{
			foreach (var s in pcm)
				pending.Enqueue(s);
			var cap = VoiceFormat.SampleRate / 5;
			while (pending.Count > cap)
				pending.Dequeue();
		}
	}

	public void Process(short[] mic)
	{
		lock (gate)
		{
			for (var n = 0; n < mic.Length; n++)
			{
				var far = pending.Count > 0 ? pending.Dequeue() : (short)0;
				Push(far);
				mic[n] = ProcessSample(mic[n]);
			}
		}
	}

	void Push(float sample)
	{
		writePos = writePos == 0 ? historyLength - 1 : writePos - 1;
		history[writePos] = sample;
		history[writePos + historyLength] = sample;
	}

	short ProcessSample(short micSample)
	{
		var refStart = writePos + delaySamples;
		var reference = new ReadOnlySpan<float>(history, refStart, taps);
		var weightSpan = new Span<float>(weights);
		var estimate = Dot(weightSpan, reference);
		var error = micSample - estimate;

		var energy = Dot(reference, reference);
		var peakRef = 0f;
		for (var i = 0; i < taps; i += 16)
			peakRef = Math.Max(peakRef, Math.Abs(reference[i]));
		var doubleTalk = Math.Abs(micSample) > DoubleTalkRatio * peakRef;
		if (peakRef > 50 && !doubleTalk)
		{
			var mu = Step * error / (energy + Regularisation);
			AddScaled(weightSpan, reference, mu);
		}

		if (peakRef > 50)
		{
			echoEnergy = echoEnergy * 0.9995 + (double)micSample * micSample;
			residualEnergy = residualEnergy * 0.9995 + (double)error * error;
		}

		return (short)Math.Clamp(error, short.MinValue, short.MaxValue);
	}

	static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
	{
		var acc = Vector<float>.Zero;
		var width = Vector<float>.Count;
		for (var i = 0; i < a.Length; i += width)
			acc += new Vector<float>(a.Slice(i, width)) * new Vector<float>(b.Slice(i, width));
		return Vector.Sum(acc);
	}

	static void AddScaled(Span<float> target, ReadOnlySpan<float> source, float factor)
	{
		var width = Vector<float>.Count;
		for (var i = 0; i < target.Length; i += width)
			(new Vector<float>(target.Slice(i, width)) + new Vector<float>(source.Slice(i, width)) * factor).CopyTo(target.Slice(i, width));
	}
}
