namespace CallBridge.Core.Appearance;

public enum WindowMaterial
{
	Solid,
	Mica,
}

public readonly record struct DisplaySettings(bool HighContrast, bool TransparencyEnabled, int WindowsBuild);

public static class MaterialPolicy
{
	public const int MinimumMicaBuild = 22621;

	public static WindowMaterial Choose(DisplaySettings settings)
	{
		if (settings.HighContrast || !settings.TransparencyEnabled)
			return WindowMaterial.Solid;
		return settings.WindowsBuild >= MinimumMicaBuild ? WindowMaterial.Mica : WindowMaterial.Solid;
	}
}
