using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable arbitrary-point master/R/G/B editor with transactional pointer and keyboard input.</summary>
public sealed class PointCurveEditor : UserControl
{
    private sealed class Plot(PointCurveEditor owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Draw(canvas, area);
    }
    private readonly Plot _plot;
    private readonly AdjustmentSlider _input, _output;
    private readonly LightButton[] _channels;
    private readonly LightButton _interpolation;
    private ChannelCurves _value = new();
    private ChannelCurves? _before;
    private CompiledPointCurve _compiled = new PointCurve().Compile();
    private CurveChannel _channel;
    private int _selected;
    private bool _dragging;
    public FrameworkElement PlotElement => _plot;
    public CurveChannel Channel => _channel;
    public int SelectedIndex => _selected;
    public ChannelCurves Value
    {
        get => _value;
        set
        {
            var normalized = value.Normalize();
            if (_value.ValueEquals(normalized)) return;
            _value = normalized; Refresh();
        }
    }
    public event Action<ChannelCurves>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public PointCurveEditor()
    {
        IsTabStop = true;
        AutomationProperties.SetName(this, "RGB point curves");
        var root = new StackPanel { Spacing = 6 };
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        _channels = Enum.GetValues<CurveChannel>().Select(channel =>
        {
            var button = new LightButton("Curve " + channel, text: channel == CurveChannel.Master ? "RGB" : channel.ToString(), action: () =>
            {
                CancelPointer(); _channel = channel; _selected = 0; Refresh();
            }) { Padding = new(7, 5, 7, 5) };
            tabs.Children.Add(button); return button;
        }).ToArray();
        root.Children.Add(tabs);
        _plot = new(this) { Height = 220 }; root.Children.Add(_plot);
        _input = new("Input", 0, 255, 0, .1); _output = new("Output", 0, 255, 0, .1);
        _input.ValueChanged += value => MovePoint((float)value / 255, Current.Points[_selected].Y);
        _output.ValueChanged += value => MovePoint(Current.Points[_selected].X, (float)value / 255);
        foreach (var slider in new[] { _input, _output })
        {
            slider.ValueCommitted += () => Committed?.Invoke();
            slider.GestureCanceled += () => Canceled?.Invoke(); root.Children.Add(slider);
        }
        var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        tools.Children.Add(new LightButton("Delete curve point", text: "Delete", action: Delete));
        tools.Children.Add(new LightButton("Reset curve channel", text: "Reset", action: () => Apply(new(), true)));
        _interpolation = new("Curve interpolation", text: "Smooth", action: () => Apply(Current with
        {
            Interpolation = Current.Interpolation == CurveInterpolation.Smooth ? CurveInterpolation.Linear : CurveInterpolation.Smooth
        }, true));
        tools.Children.Add(_interpolation); root.Children.Add(tools);
        var note = Theme.Text("Click to add; drag to reshape. Delete removes an interior point.", 10, true);
        note.TextWrapping = TextWrapping.Wrap; note.TextTrimming = TextTrimming.None; root.Children.Add(note);
        Content = root;
        _plot.PointerPressed += (_, e) =>
        {
            Focus(FocusState.Pointer); _before = _value;
            var screen = e.GetCurrentPoint(_plot).Position; var point = FromScreen(screen);
            var points = Current.Points; var nearest = -1; var distance = 100d;
            for (var i = 0; i < points.Length; i++)
            {
                var p = ToScreen(points[i]); var dx = p.X - screen.X; var dy = p.Y - screen.Y;
                if (dx * dx + dy * dy < distance) { distance = dx * dx + dy * dy; nearest = i; }
            }
            if (nearest >= 0) _selected = nearest;
            else
            {
                var close = Array.FindIndex(points, p => Math.Abs(p.X - point.X) < PointCurve.MinimumSeparation);
                if (close >= 0) _selected = close;
                else if (points.Length < PointCurve.MaximumPoints && point.X > 0 && point.X < 1)
                {
                    var list = points.ToList(); _selected = list.FindIndex(p => p.X > point.X);
                    if (_selected < 0) _selected = list.Count - 1;
                    list.Insert(_selected, point); Apply(Current with { Points = list.ToArray() }, false);
                }
                else { _before = null; e.Handled = true; return; }
            }
            _dragging = true; _plot.CapturePointer(e.Pointer); Refresh(); e.Handled = true;
        };
        _plot.PointerMoved += (_, e) =>
        {
            if (!_dragging) return; var p = FromScreen(e.GetCurrentPoint(_plot).Position); MovePoint(p.X, p.Y); e.Handled = true;
        };
        _plot.PointerReleased += (_, e) =>
        {
            if (!_dragging) return;
            var p = FromScreen(e.GetCurrentPoint(_plot).Position); MovePoint(p.X, p.Y);
            _dragging = false; _before = null; _plot.ReleasePointerCapture(e.Pointer); Committed?.Invoke(); e.Handled = true;
        };
        _plot.PointerCanceled += (_, _) => CancelPointer();
        _plot.PointerCaptureLost += (_, _) => CancelPointer();
        KeyDown += (_, e) =>
        {
            if (e.OriginalSource is TextBox) return;
            if (e.Key == VirtualKey.Escape && _dragging) { CancelPointer(); e.Handled = true; return; }
            if (e.Key == VirtualKey.Delete || e.Key == VirtualKey.Back) { Delete(); e.Handled = true; return; }
            var p = Current.Points[_selected];
            switch (e.Key)
            {
                case VirtualKey.Left: MovePoint(p.X - 1f / 255, p.Y); break;
                case VirtualKey.Right: MovePoint(p.X + 1f / 255, p.Y); break;
                case VirtualKey.Up: MovePoint(p.X, p.Y + 1f / 255); break;
                case VirtualKey.Down: MovePoint(p.X, p.Y - 1f / 255); break;
                default: return;
            }
            Committed?.Invoke(); e.Handled = true;
        };
        Refresh();
    }
    private PointCurve Current => _value.Get(_channel);
    private CurvePoint FromScreen(Point p) => new(Numeric.Unit((float)((p.X - 10) / Math.Max(1, _plot.ActualWidth - 20))), Numeric.Unit(1 - (float)((p.Y - 10) / Math.Max(1, _plot.ActualHeight - 20))));
    private Point ToScreen(CurvePoint p) => new(10 + p.X * Math.Max(1, _plot.ActualWidth - 20), 10 + (1 - p.Y) * Math.Max(1, _plot.ActualHeight - 20));
    private void MovePoint(float x, float y)
    {
        var points = (CurvePoint[])Current.Points.Clone();
        x = _selected == 0 ? 0 : _selected == points.Length - 1 ? 1 : Math.Clamp(x, points[_selected - 1].X + PointCurve.MinimumSeparation, points[_selected + 1].X - PointCurve.MinimumSeparation);
        var point = new CurvePoint(x, Numeric.Unit(y)); if (point == points[_selected]) return;
        points[_selected] = point; Apply(Current with { Points = points }, false);
    }
    private void Apply(PointCurve curve, bool commit)
    {
        var next = _value.Set(_channel, curve);
        if (!_value.ValueEquals(next)) { _value = next; Refresh(); Previewed?.Invoke(_value); }
        if (commit) Committed?.Invoke();
    }
    private void Delete()
    {
        if (_selected <= 0 || _selected >= Current.Points.Length - 1) return;
        var points = Current.Points.Where((_, i) => i != _selected).ToArray(); _selected--;
        Apply(Current with { Points = points }, true);
    }
    private void CancelPointer()
    {
        if (!_dragging) return; _dragging = false; _plot.ReleasePointerCaptures();
        if (_before is { } before) _value = before; _before = null; Refresh(); Canceled?.Invoke();
    }
    private void Refresh()
    {
        var curve = Current; _selected = Math.Clamp(_selected, 0, curve.Points.Length - 1);
        _compiled = curve.Compile();
        _input.Value = curve.Points[_selected].X * 255; _output.Value = curve.Points[_selected].Y * 255;
        _input.IsEnabled = _selected > 0 && _selected < curve.Points.Length - 1;
        _interpolation.Text = curve.Interpolation.ToString();
        for (var i = 0; i < _channels.Length; i++) _channels[i].Selected = i == (int)_channel;
        _plot.Invalidate();
    }
    private void Draw(SKCanvas canvas, Size area)
    {
        var w = Math.Max(1, (float)area.Width - 20); var h = Math.Max(1, (float)area.Height - 20);
        canvas.Save(); canvas.Translate(10, 10);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#181818") };
        canvas.DrawRect(0, 0, w, h, paint); paint.Color = SKColor.Parse("#3c3c3c"); paint.StrokeWidth = 1;
        for (var i = 0; i <= 4; i++) { canvas.DrawLine(w * i / 4, 0, w * i / 4, h, paint); canvas.DrawLine(0, h * i / 4, w, h * i / 4, paint); }
        canvas.DrawLine(0, h, w, 0, paint);
        using var path = new SKPath(); path.MoveTo(0, h * (1 - _compiled.Evaluate(0)));
        for (var i = 1; i <= 256; i++) path.LineTo(w * i / 256, h * (1 - _compiled.Evaluate(i / 256f)));
        paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1.6f;
        paint.Color = SKColor.Parse(_channel switch { CurveChannel.Red => "#df7979", CurveChannel.Green => "#91c98a", CurveChannel.Blue => "#7aaade", _ => "#dddddd" });
        canvas.DrawPath(path, paint);
        for (var i = 0; i < Current.Points.Length; i++)
        {
            var p = Current.Points[i]; paint.Style = i == _selected ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
            canvas.DrawCircle(p.X * w, (1 - p.Y) * h, i == _selected ? 4 : 3, paint);
        }
        canvas.Restore();
    }
}
