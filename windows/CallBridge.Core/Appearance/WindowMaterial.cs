namespace CallBridge.Core.Appearance;

public enum WindowMaterial
{
	Solid,
	Mica,
}

public readonly record struct MaterialContext(int OsBuild, bool HighContrast, bool TransparencyEnabled, bool BackdropApplied = true);

public static class MaterialPolicy
{
	public const int MinimumBackdropBuild = 22621;

	public static bool BackdropSupported(int osBuild) => osBuild >= MinimumBackdropBuild;

	public static WindowMaterial Choose(MaterialContext context)
	{
		if (context.HighContrast || !context.TransparencyEnabled || !BackdropSupported(context.OsBuild))
			return WindowMaterial.Solid;
		return context.BackdropApplied ? WindowMaterial.Mica : WindowMaterial.Solid;
	}

	public static bool GlassLayer(WindowMaterial material) => material == WindowMaterial.Mica;
}
