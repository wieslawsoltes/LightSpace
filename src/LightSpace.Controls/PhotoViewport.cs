using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public enum PhotoTool { Edit, Crop, RadialMask, LinearMask, Clone }
public sealed record ViewportHandle(string Id, float X, float Y);

/// <summary>Source-normalized, transaction-aware photography canvas shared by native and browser hosts.</summary>
public sealed partial class PhotoViewport : UserControl, IDisposable
{
    private sealed class Surface(PhotoViewport owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Paint(canvas, area);
    }
    private readonly EditorSession _session;
    private readonly PhotoRenderer _renderer;
    private readonly Surface _surface;
    private SKRect _imageRect;
    private SKPoint _press, _startPan;
    private SKPoint _cloneSource = new(.3f, .5f);
    private PhotoState? _startState;
    private bool _dragging, _movingCrop, _dragComparison;
    private int _edgeX, _edgeY;
    private Guid _maskId;
    private string? _lastError;
    public PhotoTool Tool { get; private set; }
    public float Zoom { get; private set; } = 1;
    public float PanX { get; private set; }
    public float PanY { get; private set; }
    public bool Before { get; set; }
    public bool Compare { get; set; }
    public float ComparisonPosition { get; private set; } = .5f;
    public bool MaskOverlay { get; set; } = true;
    public bool ShowMaskCoverage { get; set; }
    public float CloneRadius { get; set; } = .04f;
    public int ActiveMask { get; private set; } = -1;
    public Rect ImageBounds => new(_imageRect.Left, _imageRect.Top, _imageRect.Width, _imageRect.Height);
    public event Action? ViewChanged;
    public event Action<string>? Status;
    private bool IsMaskTool => Tool is PhotoTool.RadialMask or PhotoTool.LinearMask;
    private CropSettings DisplayCrop => Tool == PhotoTool.Crop ? new() : _session.Active?.State.Crop ?? new();

    public PhotoViewport(EditorSession session, PhotoRenderer renderer)
    {
        _session = session; _renderer = renderer; _surface = new(this); Content = _surface;
        IsTabStop = true; Background = Theme.Brush("#171717");
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        AutomationProperties.SetName(this, "Photo canvas"); AutomationProperties.SetAutomationId(this, "Photo canvas");
        _surface.PointerPressed += Press; _surface.PointerMoved += Move; _surface.PointerReleased += Release;
        _surface.PointerCanceled += (_, _) => Cancel(); _surface.PointerCaptureLost += (_, _) => Cancel();
        _surface.PointerWheelChanged += Wheel;
        _surface.DoubleTapped += (_, e) => { if (Tool == PhotoTool.Edit && !Compare) ToggleZoom(); e.Handled = true; };
        KeyDown += (_, e) => { if (e.Key == VirtualKey.Escape) Cancel(); };
        SizeChanged += (_, _) => Invalidate(); _session.ViewChanged += Invalidate;
    }
    public void Invalidate() => _surface.Invalidate();
    public void SetTool(PhotoTool tool) { Cancel(); Tool = tool; if (tool == PhotoTool.Crop) Fit(); Invalidate(); ViewChanged?.Invoke(); }
    public void Fit() { Zoom = 1; PanX = PanY = 0; Invalidate(); ViewChanged?.Invoke(); }
    public void ToggleZoom()
    {
        if (_session.Active is not { } photo) return;
        var (width, height) = DisplayCrop.OutputSize(photo.Width, photo.Height);
        var fit = Math.Min(Math.Max(1, (float)ActualWidth - 52) / width, Math.Max(1, (float)ActualHeight - 48) / height);
        var scale = (float)(XamlRoot?.RasterizationScale ?? 1);
        Zoom = Math.Abs(Zoom - 1) < .001f ? Math.Clamp(1 / Math.Max(.0001f, fit * scale), .01f, 64) : 1;
        PanX = PanY = 0; Invalidate(); ViewChanged?.Invoke();
    }
    public void SetActiveMask(int index) { ActiveMask = index; Invalidate(); ViewChanged?.Invoke(); }
    private SKPoint ToSource(Point point)
    {
        var photo = _session.Active; if (photo is null || _imageRect.Width <= 0) return new();
        var transform = PhotoTransform.SourceToView(DisplayCrop, photo.Width, photo.Height, _imageRect);
        if (!transform.TryInvert(out var inverse)) return new();
        var p = inverse.MapPoint((float)point.X, (float)point.Y);
        return new(Numeric.Unit(p.X / photo.Width), Numeric.Unit(p.Y / photo.Height));
    }
    private SKPoint ToView(float x, float y)
    {
        var photo = _session.Active; if (photo is null) return new();
        return PhotoTransform.SourceToView(DisplayCrop, photo.Width, photo.Height, _imageRect).MapPoint(x * photo.Width, y * photo.Height);
    }
    private void Paint(SKCanvas canvas, Size area)
    {
        canvas.Clear(SKColor.Parse("#171717")); var photo = _session.Active; if (photo is null) return;
        try
        {
            var available = SKRect.Create(26, 24, Math.Max(1, (float)area.Width - 52), Math.Max(1, (float)area.Height - 48));
            _imageRect = PhotoTransform.Fit(DisplayCrop, photo.Width, photo.Height, available, Zoom, PanX, PanY);
            using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(0, 0, 0, 130) };
            canvas.DrawRect(new(_imageRect.Left - 1, _imageRect.Top - 1, _imageRect.Right + 2, _imageRect.Bottom + 3), paint);
            _renderer.Draw(canvas, photo, _imageRect, Before, Tool == PhotoTool.Crop, IsMaskTool && ShowMaskCoverage ? ActiveMask : -1);
            if (Compare && Tool == PhotoTool.Edit)
            {
                var divider = _imageRect.Left + _imageRect.Width * ComparisonPosition;
                canvas.Save(); canvas.ClipRect(new(_imageRect.Left, _imageRect.Top, divider, _imageRect.Bottom));
                _renderer.Draw(canvas, photo, _imageRect, true); canvas.Restore();
                paint.Color = SKColors.White; paint.StrokeWidth = 1; canvas.DrawLine(divider, _imageRect.Top, divider, _imageRect.Bottom, paint);
                paint.Color = new SKColor(30, 30, 30, 230); canvas.DrawCircle(divider, _imageRect.MidY, 14, paint);
                paint.Color = SKColors.White; canvas.DrawLine(divider - 4, _imageRect.MidY - 5, divider - 4, _imageRect.MidY + 5, paint); canvas.DrawLine(divider + 4, _imageRect.MidY - 5, divider + 4, _imageRect.MidY + 5, paint);
            }
            if (Tool == PhotoTool.Crop) PaintCrop(canvas, photo.State.Crop);
            if (IsMaskTool && MaskOverlay) PaintMasks(canvas, photo);
            if (Tool == PhotoTool.Clone)
            {
                var source = ToView(_cloneSource.X, _cloneSource.Y); paint.Style = SKPaintStyle.Stroke; paint.Color = SKColor.Parse("#d5e9fa"); paint.StrokeWidth = 1;
                canvas.DrawCircle(source, 8, paint); canvas.DrawLine(source.X - 12, source.Y, source.X + 12, source.Y, paint); canvas.DrawLine(source.X, source.Y - 12, source.X, source.Y + 12, paint);
                foreach (var spot in photo.State.CloneSpots) { var p = ToView(spot.X, spot.Y); canvas.DrawCircle(p, Math.Max(4, spot.Radius * _imageRect.Height), paint); }
            }
        }
        catch (Exception error) { if (_lastError != error.Message) { _lastError = error.Message; Status?.Invoke(error.Message); } }
    }
    private void PaintCrop(SKCanvas canvas, CropSettings crop)
    {
        var a = ToView(crop.Left, crop.Top); var b = ToView(crop.Right, crop.Bottom); var rect = new SKRect(a.X, a.Y, b.X, b.Y);
        using var p = new SKPaint { Color = new SKColor(0, 0, 0, 155), IsAntialias = true };
        canvas.Save(); canvas.ClipRect(_imageRect); canvas.ClipRect(rect, SKClipOperation.Difference); canvas.DrawRect(_imageRect, p); canvas.Restore();
        p.Color = SKColors.White; p.Style = SKPaintStyle.Stroke; p.StrokeWidth = 1; canvas.DrawRect(rect, p);
        p.Color = SKColors.White.WithAlpha(130);
        for (var i = 1; i <= 2; i++) { canvas.DrawLine(rect.Left + rect.Width * i / 3, rect.Top, rect.Left + rect.Width * i / 3, rect.Bottom, p); canvas.DrawLine(rect.Left, rect.Top + rect.Height * i / 3, rect.Right, rect.Top + rect.Height * i / 3, p); }
        p.Style = SKPaintStyle.Fill; p.Color = SKColors.White;
        foreach (var x in new[] { rect.Left, rect.MidX, rect.Right }) foreach (var y in new[] { rect.Top, rect.MidY, rect.Bottom }) if (x != rect.MidX || y != rect.MidY) canvas.DrawRect(x - 3, y - 3, 6, 6, p);
    }
    private void Press(object sender, PointerRoutedEventArgs e)
    {
        if (_session.Active is not { } photo) return; var screen = e.GetCurrentPoint(_surface).Position;
        Focus(FocusState.Pointer); e.Handled = true; var p = ToSource(screen);
        if (Tool == PhotoTool.Clone)
        {
            if (!_imageRect.Contains((float)screen.X, (float)screen.Y)) return;
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu)) { _cloneSource = p; Invalidate(); Status?.Invoke("Clone source set. Click a destination."); return; }
            if (photo.State.CloneSpots.Length >= 32) { Status?.Invoke("The current image supports up to 32 clone spots."); return; }
            _session.Edit("Clone spot", s => s with { CloneSpots = [.. s.CloneSpots, new(p.X, p.Y, _cloneSource.X, _cloneSource.Y, CloneRadius)] }); return;
        }
        _maskAction = null;
        if (IsMaskTool)
        {
            if (!e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift)) _maskAction = HitMask(screen);
            if (_maskAction is null && photo.State.Masks.Length >= 8) { Status?.Invoke("Up to eight masks are supported. Existing mask handles remain editable."); return; }
            if (_maskAction is null && !_imageRect.Contains((float)screen.X, (float)screen.Y)) return;
        }
        _dragging = true; _surface.CapturePointer(e.Pointer); _press = p; _startPan = new(PanX, PanY); _startState = photo.State;
        _dragComparison = Tool == PhotoTool.Edit && Compare && Math.Abs(screen.X - (_imageRect.Left + _imageRect.Width * ComparisonPosition)) < 15;
        if (Tool == PhotoTool.Edit) { _press = new((float)screen.X, (float)screen.Y); return; }
        _session.BeginGesture();
        if (Tool == PhotoTool.Crop)
        {
            var c = photo.State.Crop; var l = ToView(c.Left, c.Top); var r = ToView(c.Right, c.Bottom);
            _edgeX = Math.Abs(screen.X - l.X) < 11 ? -1 : Math.Abs(screen.X - r.X) < 11 ? 1 : 0;
            _edgeY = Math.Abs(screen.Y - l.Y) < 11 ? -1 : Math.Abs(screen.Y - r.Y) < 11 ? 1 : 0;
            _movingCrop = _edgeX == 0 && _edgeY == 0 && (c.Width < .99f || c.Height < .99f) && p.X > c.Left && p.X < c.Right && p.Y > c.Top && p.Y < c.Bottom;
        }
        else if (_maskAction is null) { _maskId = Guid.NewGuid(); ActiveMask = photo.State.Masks.Length; }
        Move(sender, e);
    }
    private void Move(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging || _startState is null) return; var screen = e.GetCurrentPoint(_surface).Position;
        if (_dragComparison) { ComparisonPosition = Numeric.Clamp(((float)screen.X - _imageRect.Left) / Math.Max(1, _imageRect.Width), .03f, .97f); Invalidate(); e.Handled = true; return; }
        if (Tool == PhotoTool.Edit) { PanX = _startPan.X + (float)screen.X - _press.X; PanY = _startPan.Y + (float)screen.Y - _press.Y; ConstrainPan(); Invalidate(); return; }
        var p = ToSource(screen); var start = _startState;
        if (Tool == PhotoTool.Crop)
        {
            var c = start.Crop; float l = c.Left, t = c.Top, r = c.Right, b = c.Bottom;
            if (_movingCrop) { var dx = Math.Clamp(p.X - _press.X, -l, 1 - r); var dy = Math.Clamp(p.Y - _press.Y, -t, 1 - b); l += dx; r += dx; t += dy; b += dy; }
            else if (_edgeX != 0 || _edgeY != 0) { if (_edgeX < 0) l = Math.Min(p.X, r - .01f); if (_edgeX > 0) r = Math.Max(p.X, l + .01f); if (_edgeY < 0) t = Math.Min(p.Y, b - .01f); if (_edgeY > 0) b = Math.Max(p.Y, t + .01f); }
            else
            {
                if (Math.Abs(p.X - _press.X) < .003f && Math.Abs(p.Y - _press.Y) < .003f) return;
                l = Math.Min(p.X, _press.X); r = Math.Max(p.X, _press.X); t = Math.Min(p.Y, _press.Y); b = Math.Max(p.Y, _press.Y);
            }
            _session.Preview(s => s with { Crop = c with { Left = l, Top = t, Right = r, Bottom = b } });
        }
        else MoveMask(p, start);
        e.Handled = true;
    }
    private void Release(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return; Move(sender, e); _dragging = false; _surface.ReleasePointerCapture(e.Pointer);
        if (Tool != PhotoTool.Edit) _session.CommitGesture(Tool == PhotoTool.Crop ? "Crop" : _maskAction is null ? "Create gradient mask" : "Transform mask");
        _startState = null; _dragComparison = false; ViewChanged?.Invoke(); e.Handled = true;
    }
    private void Cancel()
    {
        if (!_dragging) return; _dragging = false; _surface.ReleasePointerCaptures(); _session.CancelGesture(); _startState = null; _dragComparison = false;
        ActiveMask = Math.Clamp(ActiveMask, -1, (_session.Active?.State.Masks.Length ?? 0) - 1); Invalidate();
    }
    private void Wheel(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(_surface); var factor = pointer.Properties.MouseWheelDelta > 0 ? 1.15f : 1 / 1.15f; var old = Zoom; Zoom = Math.Clamp(Zoom * factor, .01f, 64);
        var x = (float)pointer.Position.X; var y = (float)pointer.Position.Y;
        PanX = (PanX + (float)ActualWidth / 2 - x) * (Zoom / old) + x - (float)ActualWidth / 2;
        PanY = (PanY + (float)ActualHeight / 2 - y) * (Zoom / old) + y - (float)ActualHeight / 2;
        ConstrainPan(); Invalidate(); ViewChanged?.Invoke(); e.Handled = true;
    }
    private void ConstrainPan()
    {
        if (_session.Active is not { } photo) return;
        var (w, h) = DisplayCrop.OutputSize(photo.Width, photo.Height);
        var aw = Math.Max(1, (float)ActualWidth - 52); var ah = Math.Max(1, (float)ActualHeight - 48);
        var scale = Math.Min(aw / w, ah / h) * Zoom;
        PanX = Math.Clamp(PanX, -Math.Max(0, (aw + w * scale) / 2 - 32), Math.Max(0, (aw + w * scale) / 2 - 32));
        PanY = Math.Clamp(PanY, -Math.Max(0, (ah + h * scale) / 2 - 32), Math.Max(0, (ah + h * scale) / 2 - 32));
    }
    public new void Dispose() { _session.ViewChanged -= Invalidate; }
}
