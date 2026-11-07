using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CallBridge.Core.Appearance;

namespace CallBridge.Core.Tests;

public class GlassContrastTests
{
	const double Required = 4.5;
	static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
	static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

	static string ThemePath(string name, [CallerFilePath] string here = "") =>
		Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "CallBridge.App", $"Theme.{name}.xaml"));

	static (byte A, byte R, byte G, byte B) Brush(string theme, string key)
	{
		var element = XDocument.Load(ThemePath(theme)).Descendants(Presentation + "SolidColorBrush")
			.Single(e => (string?)e.Attribute(Xaml + "Key") == key);
		var hex = ((string)element.Attribute("Color")!).TrimStart('#');
		return (
			byte.Parse(hex[..2], NumberStyles.HexNumber),
			byte.Parse(hex[2..4], NumberStyles.HexNumber),
			byte.Parse(hex[4..6], NumberStyles.HexNumber),
			byte.Parse(hex[6..8], NumberStyles.HexNumber));
	}

	static (byte R, byte G, byte B) Rgb((byte A, byte R, byte G, byte B) c) => (c.R, c.G, c.B);

	static IEnumerable<(byte R, byte G, byte B)> Backdrops()
	{
		for (var gray = 0; gray <= 255; gray += 15)
			yield return ((byte)gray, (byte)gray, (byte)gray);
		foreach (var r in new byte[] { 0, 255 })
			foreach (var g in new byte[] { 0, 255 })
				foreach (var b in new byte[] { 0, 255 })
					yield return (r, g, b);
	}

	public static TheoryData<string, string> TextOnGlass => new()
	{
		{ "Light", "Label" },
		{ "Light", "LabelSecondary" },
		{ "Dark", "Label" },
		{ "Dark", "LabelSecondary" },
	};

	[Theory]
	[MemberData(nameof(TextOnGlass))]
	public void TextOnGlassKeepsContrastOverAnyBackdrop(string theme, string label)
	{
		var glass = Brush(theme, "GlassBar");
		var text = Rgb(Brush(theme, label));
		var alpha = glass.A / 255.0;
		foreach (var backdrop in Backdrops())
		{
			var plate = Contrast.Over(Rgb(glass), alpha, backdrop);
			var ratio = Contrast.Ratio(text, plate);
			Assert.True(ratio >= Required, $"{theme} {label} over {backdrop}: {ratio:F2}");
		}
	}

	[Theory]
	[MemberData(nameof(TextOnGlass))]
	public void TextOnSolidFallbackKeepsContrast(string theme, string label)
	{
		var solid = Rgb(Brush(theme, "GlassSolid"));
		var ratio = Contrast.Ratio(Rgb(Brush(theme, label)), solid);
		Assert.True(ratio >= Required, $"{theme} {label}: {ratio:F2}");
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void GlassPlateIsMostlyOpaque(string theme)
	{
		Assert.True(Brush(theme, "GlassBar").A >= 0xC0);
		Assert.Equal(0xFF, Brush(theme, "GlassSolid").A);
	}

	[Theory]
	[InlineData("Light", "GreenFill")]
	[InlineData("Light", "RedFill")]
	[InlineData("Dark", "GreenFill")]
	[InlineData("Dark", "RedFill")]
	public void ButtonLabelsOnTheirFillKeepContrast(string theme, string fill)
	{
		var ratio = Contrast.Ratio(Rgb(Brush(theme, "OnFill")), Rgb(Brush(theme, fill)));
		Assert.True(ratio >= Required, $"{theme} {fill}: {ratio:F2}");
	}
}
