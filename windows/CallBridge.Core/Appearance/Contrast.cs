namespace CallBridge.Core.Appearance;

public static class Contrast
{
	public static double Luminance((byte R, byte G, byte B) color)
	{
		static double Channel(byte value)
		{
			var c = value / 255.0;
			return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}
		return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
	}

	public static double Ratio((byte R, byte G, byte B) first, (byte R, byte G, byte B) second)
	{
		var a = Luminance(first);
		var b = Luminance(second);
		return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
	}

	public static (byte R, byte G, byte B) Over((byte R, byte G, byte B) top, double alpha, (byte R, byte G, byte B) below)
	{
		static byte Mix(byte t, byte b, double a) => (byte)Math.Round(t * a + b * (1 - a));
		return (Mix(top.R, below.R, alpha), Mix(top.G, below.G, alpha), Mix(top.B, below.B, alpha));
	}
}
