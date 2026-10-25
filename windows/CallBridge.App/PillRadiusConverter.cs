using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CallBridge.App;

/// <summary>Turns an element height into the corner radius of a perfect pill.</summary>
public sealed class PillRadiusConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		new CornerRadius(value is double height ? height / 2 : 0);

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
