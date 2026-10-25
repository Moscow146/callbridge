Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'windows\CallBridge.App\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null

# Same glyph as android/app/src/main/res/drawable/ic_foreground.xml, 24 unit grid
$handset = [Windows.Media.Geometry]::Parse('M6.6,10.8C8,13.6 10.4,15.9 13.2,17.4L15.4,15.2C15.7,14.9 16.1,14.8 16.4,15 17.5,15.4 18.7,15.6 20,15.6 20.6,15.6 21,16 21,16.6L21,20C21,20.6 20.6,21 20,21 10.6,21 3,13.4 3,4 3,3.4 3.4,3 4,3L7.5,3C8.1,3 8.5,3.4 8.5,4 8.5,5.3 8.7,6.5 9.1,7.6 9.2,7.9 9.1,8.3 8.9,8.6L6.6,10.8Z')
$wave1 = [Windows.Media.Geometry]::Parse('M13,5.5A5,5 0 0 1 18,10.5')
$wave2 = [Windows.Media.Geometry]::Parse('M13,2A8.5,8.5 0 0 1 21.5,10.5')

function New-Brush([string]$hex) { [Windows.Media.SolidColorBrush][Windows.Media.ColorConverter]::ConvertFromString($hex) }

function Render-Icon([int]$size) {
	$visual = New-Object Windows.Media.DrawingVisual
	$dc = $visual.RenderOpen()
	$unit = $size / 256.0
	$dc.PushTransform((New-Object Windows.Media.ScaleTransform $unit, $unit))

	$bg = New-Object Windows.Media.LinearGradientBrush
	$bg.StartPoint = New-Object Windows.Point 0, 0
	$bg.EndPoint = New-Object Windows.Point 1, 1
	foreach ($stop in @(@('#3B82F6', 0.0), @('#7C3AED', 0.55), @('#EC4899', 1.0))) {
		$bg.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.ColorConverter]::ConvertFromString($stop[0])), $stop[1]))
	}
	$body = New-Object Windows.Rect 8, 8, 240, 240
	$dc.DrawRoundedRectangle($bg, $null, $body, 58, 58)

	$sheen = New-Object Windows.Media.LinearGradientBrush
	$sheen.StartPoint = New-Object Windows.Point 0.5, 0
	$sheen.EndPoint = New-Object Windows.Point 0.5, 0.6
	$sheen.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(110, 255, 255, 255)), 0.0))
	$sheen.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(0, 255, 255, 255)), 1.0))
	$dc.DrawRoundedRectangle($sheen, $null, $body, 58, 58)

	$rim = New-Object Windows.Media.LinearGradientBrush
	$rim.StartPoint = New-Object Windows.Point 0, 0
	$rim.EndPoint = New-Object Windows.Point 1, 1
	$rim.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(200, 255, 255, 255)), 0.0))
	$rim.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(30, 255, 255, 255)), 0.5))
	$rim.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(110, 255, 255, 255)), 1.0))
	$dc.DrawRoundedRectangle($null, (New-Object Windows.Media.Pen $rim, 4), $body, 58, 58)

	$glyph = 8.2
	$offset = (256 - 24 * $glyph) / 2
	$dc.PushTransform((New-Object Windows.Media.TranslateTransform $offset, $offset))
	$dc.PushTransform((New-Object Windows.Media.ScaleTransform $glyph, $glyph))
	$white = New-Brush '#FFFFFFFF'
	$dc.DrawGeometry($white, $null, $handset)
	$pen = New-Object Windows.Media.Pen $white, 1.7
	$pen.StartLineCap = 'Round'
	$pen.EndLineCap = 'Round'
	$dc.DrawGeometry($null, $pen, $wave1)
	$dc.DrawGeometry($null, $pen, $wave2)
	$dc.Pop()
	$dc.Pop()
	$dc.Pop()
	$dc.Close()

	$bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([Windows.Media.PixelFormats]::Pbgra32)
	$bitmap.Render($visual)
	$encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
	$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
	$stream = New-Object IO.MemoryStream
	$encoder.Save($stream)
	return , $stream.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = foreach ($s in $sizes) { , (Render-Icon $s) }

$ico = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $ico
$w.Write([uint16]0)
$w.Write([uint16]1)
$w.Write([uint16]$sizes.Count)
$dataOffset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
	$s = $sizes[$i]
	$w.Write([byte]($s % 256))
	$w.Write([byte]($s % 256))
	$w.Write([byte]0)
	$w.Write([byte]0)
	$w.Write([uint16]1)
	$w.Write([uint16]32)
	$w.Write([uint32]$images[$i].Length)
	$w.Write([uint32]$dataOffset)
	$dataOffset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $assets 'app.ico'), $ico.ToArray())

[IO.File]::WriteAllBytes((Join-Path $assets 'app.png'), (Render-Icon 256))
$docs = Join-Path $root 'docs'
[IO.File]::WriteAllBytes((Join-Path $docs 'icon.png'), (Render-Icon 512))
Write-Host "Icons written to $assets and $docs"
