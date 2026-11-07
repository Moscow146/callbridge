using System.Xml.Linq;
using CallBridge.Core.Appearance;

namespace CallBridge.Core.Tests;

public class WindowMaterialTests
{
	[Fact]
	public void MicaWhenWindows11AndTransparencyOn()
	{
		var material = MaterialPolicy.Choose(new DisplaySettings(false, true, 22631));
		Assert.Equal(WindowMaterial.Mica, material);
	}

	[Fact]
	public void MicaStartsAtTheFirstBuildWithSystemBackdrop()
	{
		Assert.Equal(WindowMaterial.Mica, MaterialPolicy.Choose(new DisplaySettings(false, true, MaterialPolicy.MinimumMicaBuild)));
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new DisplaySettings(false, true, MaterialPolicy.MinimumMicaBuild - 1)));
	}

	[Theory]
	[InlineData(19045)]
	[InlineData(22000)]
	public void SolidOnWindows10AndEarlyWindows11(int build)
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new DisplaySettings(false, true, build)));
	}

	[Fact]
	public void SolidInHighContrastEvenWithTransparency()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new DisplaySettings(true, true, 26200)));
	}

	[Fact]
	public void SolidWhenTransparencyEffectsAreOff()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new DisplaySettings(false, false, 26200)));
	}

	[Fact]
	public void SolidWhenEverythingIsAgainstGlass()
	{
		Assert.Equal(WindowMaterial.Solid, MaterialPolicy.Choose(new DisplaySettings(true, false, 19045)));
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void TextOnGlassKeepsContrastOverAnyBackdrop(string theme)
	{
		var colors = ReadColors(theme);
		var tint = colors["GlassTint"];
		var backdrops = new[] { new Rgba(255, 0, 0, 0), new Rgba(255, 255, 255, 255), new Rgba(255, 128, 128, 128) };
		foreach (var backdrop in backdrops)
		{
			var plate = tint.Over(backdrop);
			foreach (var text in new[] { "Label", "LabelSecondary" })
				Assert.True(Contrast.Ratio(colors[text], plate) >= 4.5, $"{theme} {text} over backdrop {backdrop}: {Contrast.Ratio(colors[text], plate):0.00}");
		}
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void SolidFallbackKeepsContrast(string theme)
	{
		var colors = ReadColors(theme);
		foreach (var text in new[] { "Label", "LabelSecondary" })
			Assert.True(Contrast.Ratio(colors[text], colors["GlassSolid"]) >= 4.5);
	}

	[Fact]
	public void ContrastMatchesKnownValues()
	{
		var black = new Rgba(255, 0, 0, 0);
		var white = new Rgba(255, 255, 255, 255);
		Assert.Equal(21, Contrast.Ratio(black, white), 3);
		Assert.Equal(1, Contrast.Ratio(white, white), 3);
	}

	static Dictionary<string, Rgba> ReadColors(string theme)
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Themes", $"Theme.{theme}.xaml");
		var xaml = XDocument.Load(path);
		XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
		return xaml.Root!.Elements()
			.Where(e => e.Name.LocalName == "SolidColorBrush")
			.ToDictionary(e => (string)e.Attribute(x + "Key")!, e => Rgba.Parse((string)e.Attribute("Color")!));
	}
}
