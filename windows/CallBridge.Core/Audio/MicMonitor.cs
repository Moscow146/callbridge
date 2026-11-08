namespace CallBridge.Core.Audio;

public sealed class MicMonitor : IDisposable
{
	MicCapture? capture;

	public bool Running => capture != null;

	public void Start(string? deviceId)
	{
		Stop();
		try
		{
			var next = new MicCapture(deviceId);
			next.Start();
			capture = next;
		}
		catch (Exception)
		{
			capture = null;
		}
	}

	public void Stop()
	{
		var old = capture;
		capture = null;
		old?.Dispose();
	}

	public float TakePeak() => capture?.TakePeak() ?? 0f;

	public void Dispose() => Stop();
}
