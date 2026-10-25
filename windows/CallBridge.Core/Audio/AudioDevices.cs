using NAudio.CoreAudioApi;

namespace CallBridge.Core.Audio;

public sealed record AudioDeviceInfo(string Id, string Name)
{
	public override string ToString() => Name;
}

public static class AudioDevices
{
	public static IReadOnlyList<AudioDeviceInfo> Capture() => List(DataFlow.Capture);

	public static IReadOnlyList<AudioDeviceInfo> Render() => List(DataFlow.Render);

	static List<AudioDeviceInfo> List(DataFlow flow)
	{
		using var enumerator = new MMDeviceEnumerator();
		return enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active)
			.Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName))
			.ToList();
	}

	public static MMDevice Open(DataFlow flow, string? id)
	{
		var enumerator = new MMDeviceEnumerator();
		if (id != null)
		{
			try
			{
				return enumerator.GetDevice(id);
			}
			catch (Exception)
			{
			}
		}
		return enumerator.GetDefaultAudioEndpoint(flow, Role.Communications);
	}
}
