using System.Globalization;

namespace CallBridge.Core.Appearance;

public readonly record struct Rgba(byte A, byte R, byte G, byte B)
{
	public static Rgba Parse(string argb)
	{
		var hex = argb.TrimStart('#');
		if (hex.Length == 6)
			hex = "FF" + hex;
		var value = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		return new Rgba((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
	}

	public Rgba Over(Rgba backdrop)
	{
		var alpha = A / 255.0;
		byte Mix(byte top, byte bottom) => (byte)Math.Round(top * alpha + bottom * (1 - alpha));
		return new Rgba(255, Mix(R, backdrop.R), Mix(G, backdrop.G), Mix(B, backdrop.B));
	}
}

public static class Contrast
{
	public static double Luminance(Rgba color)
	{
		static double Channel(byte value)
		{
			var c = value / 255.0;
			return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}
		return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
	}

	public static double Ratio(Rgba first, Rgba second)
	{
		var a = Luminance(first);
		var b = Luminance(second);
		return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
	}
}
