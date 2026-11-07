using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CallBridge.App;

public partial class MainWindow
{
	const double GlassRadius = 28;
	const double GlassBleed = 28;

	void InitGlass()
	{
		HeaderBlur.Fill = NewBlurBrush();
		ActionsBlur.Fill = NewBlurBrush();
		LayoutUpdated += (_, _) =>
		{
			UpdateGlass(HeaderGlass, HeaderBlur);
			UpdateGlass(ActionsGlass, ActionsBlur);
		};
	}

	VisualBrush NewBlurBrush() => new(Scroller)
	{
		ViewboxUnits = BrushMappingMode.Absolute,
		ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
		Viewport = new Rect(0, 0, 1, 1),
		Stretch = Stretch.Fill,
	};

	void UpdateGlass(FrameworkElement plate, Rectangle blur)
	{
		if (!plate.IsVisible || plate.ActualWidth <= 0 || plate.ActualHeight <= 0 || blur.Fill is not VisualBrush brush)
			return;
		var origin = plate.TransformToVisual(Scroller).Transform(new Point());
		var view = new Rect(origin.X - GlassBleed, origin.Y - GlassBleed, plate.ActualWidth + 2 * GlassBleed, plate.ActualHeight + 2 * GlassBleed);
		if (brush.Viewbox != view)
			brush.Viewbox = view;
		var bounds = new Rect(0, 0, plate.ActualWidth, plate.ActualHeight);
		if (plate.Clip is not RectangleGeometry clip || clip.Rect != bounds)
			plate.Clip = new RectangleGeometry(bounds, GlassRadius, GlassRadius);
	}
}
