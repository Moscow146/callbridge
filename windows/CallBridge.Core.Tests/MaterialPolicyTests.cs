using CallBridge.Core.Appearance;

namespace CallBridge.Core.Tests;

public class MaterialPolicyTests
{
	const int Windows11 = 22631;
	const int Windows11First = 22000;
	const int Windows10 = 19045;

	[Fact]
	public void MicaOnSupportedBuildWithTransparency()
	{
		Assert.Equal(WindowMaterial.Mica, MaterialPolicy.Choose(new MaterialContext(Windows11, false, true)));
	}

	[Theory]
	[InlineData(Windows10)]
	[InlineData(Windows11First)]
	[InlineData(0)]
	public void SolidWhenBackdropIsNotSupported(int build)
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new MaterialContext(build, false, true)));
	}

	[Fact]
	public void SolidInHighContrast()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new MaterialContext(Windows11, true, true)));
	}

	[Fact]
	public void SolidWhenTransparencyIsOff()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new MaterialContext(Windows11, false, false)));
	}

	[Fact]
	public void SolidWhenTheSystemRefusedTheBackdrop()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new MaterialContext(Windows11, false, true, false)));
	}

	[Fact]
	public void HighContrastBeatsEverythingElse()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new MaterialContext(Windows11, true, false)));
	}

	[Fact]
	public void SupportStartsAtTheBackdropBuild()
	{
		Assert.False(MaterialPolicy.BackdropSupported(MaterialPolicy.MinimumBackdropBuild - 1));
		Assert.True(MaterialPolicy.BackdropSupported(MaterialPolicy.MinimumBackdropBuild));
	}

	[Fact]
	public void GlassLayerFollowsTheWindowMaterial()
	{
		Assert.True(MaterialPolicy.GlassLayer(WindowMaterial.Mica));
		Assert.False(MaterialPolicy.GlassLayer(WindowMaterial.Solid));
	}

	[Fact]
	public void BlurAndFloatingGlassAppearOnlyWhenEveryConditionHolds()
	{
		foreach (var build in new[] { 0, Windows10, Windows11First, Windows11 })
			foreach (var highContrast in new[] { false, true })
				foreach (var transparency in new[] { false, true })
					foreach (var backdrop in new[] { false, true })
					{
						var material = MaterialPolicy.Choose(new MaterialContext(build, highContrast, transparency, backdrop));
						var expected = build >= MaterialPolicy.MinimumBackdropBuild && !highContrast && transparency && backdrop;
						Assert.Equal(expected, MaterialPolicy.GlassLayer(material));
					}
	}
}
