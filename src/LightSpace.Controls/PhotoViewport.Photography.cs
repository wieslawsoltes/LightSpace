using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private SKPoint? _straightenStart, _straightenEnd;
    private PointD _straightenFrameStart;
    public ClippingIndicators Clipping { get; set; }
    private bool Uncropped => Tool is PhotoTool.Crop or PhotoTool.Straighten;
    private static bool ValidSource(SKPoint p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && p.X >= 0 && p.X <= 1 && p.Y >= 0 && p.Y <= 1;
    private SKPoint FrameToView(float x, float y)
    {
        if (_session.Active is not { } photo) return new();
        return PhotoTransform.SourceToView(DisplayCrop, photo.Width, photo.Height, _imageRect).MapPoint(x * photo.Width, y * photo.Height);
    }
    private PointD ToFrame(Point point)
    {
        if (_session.Active is not { } photo) return new();
        var matrix = PhotoTransform.SourceToView(DisplayCrop, photo.Width, photo.Height, _imageRect);
        if (!matrix.TryInvert(out var inverse)) return new();
        var p = inverse.MapPoint((float)point.X, (float)point.Y); return new(p.X / photo.Width, p.Y / photo.Height);
    }
    private void PickWhiteBalance(PointerRoutedEventArgs e)
    {
        if (_session.Active is not { } photo) return;
        var point = e.GetCurrentPoint(_surface).Position;
        if (!_imageRect.Contains((float)point.X, (float)point.Y)) return;
        var source = ToSource(point); if (!ValidSource(source)) return;
        try
        {
            var sample = _renderer.SampleSource(photo, source.X, source.Y);
            var estimate = sample is null ? null : WhiteBalanceEstimator.Estimate(sample);
            if (estimate is null) { Status?.Invoke("Choose a nontransparent, nonblack neutral source area."); return; }
            _session.Edit("White balance picker", s => s with { Develop = s.Develop with { Temperature = estimate.Temperature, Tint = estimate.Tint } });
            SetTool(PhotoTool.Edit);
            Status?.Invoke(estimate.WasClamped ? "White balance reached this relative model's correction limit; refine Temperature and Tint." : "White balance sampled from the original source.");
        }
        catch (Exception error) { Status?.Invoke(error.Message); }
        e.Handled = true;
    }
    private void PressStraighten(PointerRoutedEventArgs e)
    {
        if (_session.Active is not { } photo) return;
        var p = e.GetCurrentPoint(_surface).Position;
        if (!_imageRect.Contains((float)p.X, (float)p.Y)) return;
        _startState = photo.State; _straightenFrameStart = ToFrame(p);
        _straightenStart = _straightenEnd = new((float)p.X, (float)p.Y);
        _dragging = true; Focus(FocusState.Pointer); _surface.CapturePointer(e.Pointer); _session.BeginGesture(); e.Handled = true;
    }
    private void MoveStraighten(PointerRoutedEventArgs e)
    {
        if (!_dragging || _startState is null || _session.Active is not { } photo) return;
        var p = e.GetCurrentPoint(_surface).Position; _straightenEnd = new((float)p.X, (float)p.Y);
        var end = ToFrame(p);
        if (Math.Abs(end.X - _straightenFrameStart.X) + Math.Abs(end.Y - _straightenFrameStart.Y) < .015) return;
        var angle = GeometryProjection.HorizonCorrection(_straightenFrameStart, end, (double)photo.Width / photo.Height);
        _session.Preview(s => s with { Geometry = _startState.Geometry with { Rotate = Math.Clamp(_startState.Geometry.Rotate + angle, -45, 45) } });
        Invalidate(); e.Handled = true;
    }
    private void PaintStraighten(SKCanvas canvas)
    {
        if (_straightenStart is not { } a || _straightenEnd is not { } b) return;
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#9ecefa"), StrokeWidth = 2, Style = SKPaintStyle.Stroke };
        canvas.DrawLine(a, b, paint); canvas.DrawCircle(a, 5, paint); canvas.DrawCircle(b, 5, paint);
    }
}
