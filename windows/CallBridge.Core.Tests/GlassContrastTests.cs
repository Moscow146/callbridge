using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CallBridge.Core.Appearance;

namespace CallBridge.Core.Tests;

public class GlassContrastTests
{
	const double Required = 4.5;
	static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
	static readonly XNamespace System = "clr-namespace:System;assembly=System.Runtime";
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

	static double Number(string theme, string key)
	{
		var element = XDocument.Load(ThemePath(theme)).Descendants(System + "Double")
			.Single(e => (string?)e.Attribute(Xaml + "Key") == key);
		return double.Parse(element.Value, CultureInfo.InvariantCulture);
	}

	static (byte A, byte R, byte G, byte B) FirstStop(string theme, string key)
	{
		var stop = XDocument.Load(ThemePath(theme)).Descendants(Presentation + "LinearGradientBrush")
			.Single(e => (string?)e.Attribute(Xaml + "Key") == key)
			.Elements(Presentation + "GradientStop").OrderBy(e => double.Parse((string)e.Attribute("Offset")!, CultureInfo.InvariantCulture)).First();
		var hex = ((string)stop.Attribute("Color")!).TrimStart('#');
		return (
			byte.Parse(hex[..2], NumberStyles.HexNumber),
			byte.Parse(hex[2..4], NumberStyles.HexNumber),
			byte.Parse(hex[4..6], NumberStyles.HexNumber),
			byte.Parse(hex[6..8], NumberStyles.HexNumber));
	}

	static readonly (byte R, byte G, byte B) White = (255, 255, 255);

	static readonly string[] LargeAreas = ["WindowBg", "Surface", "Raised", "Fill", "FillHover", "FillPressed", "Separator"];
	static readonly string[] SmallAreas = ["Label", "Accent", "GreenFill", "RedFill", "Green", "Red", "Orange", "Thumb", "ThumbRim"];
	const double SmallAreaCoverage = 0.35;

	static IEnumerable<(string Name, (byte R, byte G, byte B) Color)> BlurredContent(string theme)
	{
		yield return ("QR tile", White);
		foreach (var large in LargeAreas)
		{
			var color = Rgb(Brush(theme, large));
			yield return (large, color);
			foreach (var small in SmallAreas)
				yield return ($"{large} with {small}", Contrast.Over(Rgb(Brush(theme, small)), SmallAreaCoverage, color));
		}
	}

	static (byte R, byte G, byte B) GlassPlate(string theme, (byte R, byte G, byte B) content, bool withSheen)
	{
		var baseColor = Rgb(Brush(theme, "WindowBg"));
		var copy = Contrast.Over(content, Number(theme, "GlassBlurOpacity"), baseColor);
		var tint = Brush(theme, "GlassBar");
		var plate = Contrast.Over(Rgb(tint), tint.A / 255.0, copy);
		if (!withSheen)
			return plate;
		var sheen = FirstStop(theme, "GlassSheenMica");
		return Contrast.Over(Rgb(sheen), sheen.A / 255.0, plate);
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void TextOnGlassKeepsContrastOverTheWorstBlurredContent(string theme)
	{
		var text = Rgb(Brush(theme, "Label"));
		foreach (var (name, content) in BlurredContent(theme))
		{
			var ratio = Contrast.Ratio(text, GlassPlate(theme, content, true));
			Assert.True(ratio >= Required, $"{theme} Label over {name}: {ratio:F2}");
		}
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void SecondaryTextOnGlassKeepsContrastOverCardsAndFills(string theme)
	{
		var text = Rgb(Brush(theme, "LabelSecondary"));
		foreach (var large in LargeAreas)
		{
			var ratio = Contrast.Ratio(text, GlassPlate(theme, Rgb(Brush(theme, large)), true));
			Assert.True(ratio >= Required, $"{theme} LabelSecondary over {large}: {ratio:F2}");
		}
	}

	[Theory]
	[InlineData("Light", "Label")]
	[InlineData("Light", "LabelSecondary")]
	[InlineData("Dark", "Label")]
	[InlineData("Dark", "LabelSecondary")]
	public void TextOnSolidFallbackKeepsContrast(string theme, string label)
	{
		var solid = Rgb(Brush(theme, "GlassSolid"));
		var ratio = Contrast.Ratio(Rgb(Brush(theme, label)), solid);
		Assert.True(ratio >= Required, $"{theme} {label}: {ratio:F2}");
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void GlassTintIsTranslucentButNotFaint(string theme)
	{
		var alpha = Brush(theme, "GlassBar").A / 255.0;
		Assert.InRange(alpha, 0.25, 0.45);
		Assert.InRange(Number(theme, "GlassBlurOpacity"), 0.4, 1.0);
		Assert.Equal(0xFF, Brush(theme, "GlassSolid").A);
		Assert.Equal(0xFF, Brush(theme, "WindowBg").A);
	}

	[Theory]
	[InlineData("Light")]
	[InlineData("Dark")]
	public void GlassRimIsBrighterAtTheTop(string theme)
	{
		var document = XDocument.Load(ThemePath(theme));
		var stops = document.Descendants(Presentation + "LinearGradientBrush")
			.Single(e => (string?)e.Attribute(Xaml + "Key") == "GlassRimMica")
			.Elements(Presentation + "GradientStop")
			.OrderBy(e => double.Parse((string)e.Attribute("Offset")!, CultureInfo.InvariantCulture))
			.Select(e => ((string)e.Attribute("Color")!).TrimStart('#')[..2])
			.Select(h => byte.Parse(h, NumberStyles.HexNumber))
			.ToArray();
		Assert.True(stops[0] > stops[^1]);
	}

	[Fact]
	public void HeaderAndCallBarTextUsesThePrimaryLabelColor()
	{
		var window = XDocument.Load(WindowPath());
		var status = window.Descendants(Presentation + "TextBlock").Single(e => (string?)e.Attribute(Xaml + "Name") == "StatusText");
		Assert.Equal("{DynamicResource Label}", (string?)status.Attribute("Foreground"));
	}

	static string WindowPath([CallerFilePath] string here = "") =>
		Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "CallBridge.App", "MainWindow.xaml"));

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
