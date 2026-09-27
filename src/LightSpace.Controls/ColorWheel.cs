using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable hue/saturation wheel with captured pointer gestures and keyboard editing.</summary>
public sealed class ColorWheel : UserControl
{
    private sealed class WheelSurface(ColorWheel owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Draw(canvas, area);
    }
    private readonly WheelSurface _surface;
    private GradingTone _value = new();
    private bool _dragging;
    private SKShader? _hues, _saturation;
    private float _shaderRadius;
    public GradingTone Value
    {
        get => _value;
        set { var normalized = value.Normalize(); if (normalized == _value) return; _value = normalized; _surface.Invalidate(); }
    }
    public event Action<GradingTone>? ValueChanged;
    public event Action? Committed;
    public event Action? Canceled;
    public ColorWheel()
    {
        IsTabStop = true; Height = 166; _surface = new(this); Content = _surface;
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        AutomationProperties.SetName(this, "Color grading wheel");
        ToolTipService.SetToolTip(this, "Drag hue and saturation. Arrow keys adjust hue/saturation; Escape cancels; double-click resets color.");
        _surface.PointerPressed += (_, e) => { Focus(FocusState.Pointer); _dragging = true; _surface.CapturePointer(e.Pointer); Move(e); e.Handled = true; };
        _surface.PointerMoved += (_, e) => { if (_dragging) { Move(e); e.Handled = true; } };
        _surface.PointerReleased += (_, e) => { if (!_dragging) return; Move(e); _dragging = false; _surface.ReleasePointerCapture(e.Pointer); Committed?.Invoke(); e.Handled = true; };
        _surface.PointerCanceled += (_, _) => Cancel(); _surface.PointerCaptureLost += (_, _) => Cancel();
        _surface.DoubleTapped += (_, e) => { SetFromInput(Value with { Hue = 0, Saturation = 0 }); Committed?.Invoke(); e.Handled = true; };
        KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape && _dragging) { Cancel(); e.Handled = true; return; }
            var next = e.Key switch
            {
                VirtualKey.Left => Value with { Hue = Numeric.Angle(Value.Hue - 1) },
                VirtualKey.Right => Value with { Hue = Numeric.Angle(Value.Hue + 1) },
                VirtualKey.Up => Value with { Saturation = Math.Min(100, Value.Saturation + 1) },
                VirtualKey.Down => Value with { Saturation = Math.Max(0, Value.Saturation - 1) },
                VirtualKey.Home => Value with { Hue = 0, Saturation = 0 }, _ => null
            };
            if (next is null) return; SetFromInput(next); Committed?.Invoke(); e.Handled = true;
        };
        GotFocus += (_, _) => _surface.Invalidate(); LostFocus += (_, _) => _surface.Invalidate();
        Unloaded += (_, _) => { _hues?.Dispose(); _saturation?.Dispose(); _hues = _saturation = null; };
    }
    private void Cancel()
    {
        if (!_dragging) return; _dragging = false; _surface.ReleasePointerCaptures(); Canceled?.Invoke();
    }
    private void SetFromInput(GradingTone value) { Value = value; ValueChanged?.Invoke(Value); }
    private void Move(PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(_surface).Position;
        var radius = Math.Max(1, Math.Min(_surface.ActualWidth, _surface.ActualHeight) / 2 - 14);
        var x = p.X - _surface.ActualWidth / 2; var y = p.Y - _surface.ActualHeight / 2;
        var hue = Numeric.Angle((float)(Math.Atan2(y, x) * 180 / Math.PI));
        var saturation = (float)Math.Min(100, Math.Sqrt(x * x + y * y) / radius * 100);
        SetFromInput(Value with { Hue = hue, Saturation = saturation });
    }
    private void Draw(SKCanvas canvas, Size area)
    {
        var radius = Math.Max(1, (float)Math.Min(area.Width, area.Height) / 2 - 14);
        if (_hues is null || _shaderRadius != radius)
        {
            _hues?.Dispose(); _saturation?.Dispose(); _shaderRadius = radius;
            _hues = SKShader.CreateSweepGradient(new SKPoint(0, 0), new SKColor[] { SKColors.Red, SKColors.Yellow, SKColors.Lime, SKColors.Cyan, SKColors.Blue, SKColors.Magenta, SKColors.Red }, null);
            _saturation = SKShader.CreateRadialGradient(new SKPoint(0, 0), radius, new SKColor[] { SKColors.White, SKColors.White.WithAlpha(0) }, null, SKShaderTileMode.Clamp);
        }
        canvas.Save(); canvas.Translate((float)area.Width / 2, (float)area.Height / 2);
        using var paint = new SKPaint { IsAntialias = true, Shader = _hues }; canvas.DrawCircle(0, 0, radius, paint);
        paint.Shader = _saturation; canvas.DrawCircle(0, 0, radius, paint); paint.Shader = null;
        paint.Color = SKColor.Parse("#151515"); paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 2; canvas.DrawCircle(0, 0, radius, paint);
        var a = Value.Hue * MathF.PI / 180; var r = Value.Saturation / 100 * radius; var x = MathF.Cos(a) * r; var y = MathF.Sin(a) * r;
        paint.StrokeWidth = 3; paint.Color = SKColor.Parse("#161616"); canvas.DrawCircle(x, y, 6, paint);
        paint.StrokeWidth = 1.5f; paint.Color = SKColors.White; canvas.DrawCircle(x, y, 6, paint);
        paint.StrokeWidth = 1; paint.Color = SKColors.Black.WithAlpha(90); canvas.DrawLine(-3, 0, 3, 0, paint); canvas.DrawLine(0, -3, 0, 3, paint);
        if (FocusState == FocusState.Keyboard) { paint.Color = SKColor.Parse("#91c3ef"); canvas.DrawCircle(0, 0, radius + 5, paint); }
        canvas.Restore();
    }
}
