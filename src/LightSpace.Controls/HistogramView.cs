using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

/// <summary>Interactive tone histogram, five drag regions and independent clipping indicators.</summary>
public sealed class HistogramView : UserControl
{
    private sealed class Plot(HistogramView owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Draw(canvas, area);
    }
    private Histogram _histogram = Histogram.Empty;
    private readonly Plot _plot;
    private readonly TextBlock _label;
    private readonly LightButton _shadow, _highlight;
    private readonly TextBlock _shadowMark = Theme.Text("△", 12), _highlightMark = Theme.Text("△", 12);
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    private bool _dragging;
    private double _startX;
    private int _region = 2;
    private ClippingIndicators _clipping;
    private static readonly string[] Tones = ["Blacks", "Shadows", "Exposure", "Highlights", "Whites"];
    public Histogram Histogram { get => _histogram; set { _histogram = value; Refresh(); } }
    public ClippingIndicators Clipping { get => _clipping; set { if (_clipping == value) return; _clipping = value; Refresh(); } }
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<ClippingIndicators>? ClippingChanged;
    public event Action<string>? AdjustmentStarted;
    public event Action<string, float>? AdjustmentDelta;
    public event Action? Committed;
    public event Action? Canceled;
    public HistogramView()
    {
        Height = 104; IsTabStop = true; HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        AutomationProperties.SetName(this, "Interactive RGB histogram");
        var root = new Grid { RowDefinitions = { new() { Height = new(1, GridUnitType.Star) }, new() { Height = new(20) } } };
        _plot = new(this); root.Children.Add(_plot); _widgets["histogram-plot"] = _plot;
        _shadow = new LightButton("Shadow clipping", action: () => Toggle(ClippingIndicators.Shadows))
        { Content = _shadowMark, Width = 21, Height = 20, MinWidth = 20, MinHeight = 20, Padding = new(3, 0, 3, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        _highlight = new LightButton("Highlight clipping", action: () => Toggle(ClippingIndicators.Highlights))
        { Content = _highlightMark, Width = 21, Height = 20, MinWidth = 20, MinHeight = 20, Padding = new(3, 0, 3, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        root.Children.Add(_shadow); root.Children.Add(_highlight); _widgets["Shadow clipping"] = _shadow; _widgets["Highlight clipping"] = _highlight;
        _label = Theme.Text("Exposure", 10, true); _label.HorizontalAlignment = HorizontalAlignment.Center; Grid.SetRow(_label, 1); root.Children.Add(_label); Content = root;
        _plot.PointerPressed += (_, e) =>
        {
            var point = e.GetCurrentPoint(_plot); if (!point.Properties.IsLeftButtonPressed) return;
            _region = Region(point.Position.X); _startX = point.Position.X; _dragging = true;
            Focus(FocusState.Pointer); _plot.CapturePointer(e.Pointer); AdjustmentStarted?.Invoke(Tones[_region]); Refresh(); e.Handled = true;
        };
        _plot.PointerMoved += (_, e) =>
        {
            var x = e.GetCurrentPoint(_plot).Position.X;
            if (_dragging) { Change(x); e.Handled = true; }
            else { var region = Region(x); if (_region != region) { _region = region; Refresh(); } }
        };
        _plot.PointerReleased += (_, e) =>
        {
            if (!_dragging) return; Change(e.GetCurrentPoint(_plot).Position.X); _dragging = false;
            _plot.ReleasePointerCapture(e.Pointer); Committed?.Invoke(); e.Handled = true;
        };
        _plot.PointerCanceled += (_, _) => Cancel(); _plot.PointerCaptureLost += (_, _) => Cancel();
        KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape && _dragging) { Cancel(); e.Handled = true; }
            else if (e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right)
            {
                var tone = Tones[_region]; AdjustmentStarted?.Invoke(tone);
                AdjustmentDelta?.Invoke(tone, (e.Key == Windows.System.VirtualKey.Left ? -1 : 1) * (_region == 2 ? .1f : 1));
                Committed?.Invoke(); e.Handled = true;
            }
        };
        Refresh();
    }
    private int Region(double x) => Math.Clamp((int)(x / Math.Max(1, _plot.ActualWidth) * 5), 0, 4);
    private void Change(double x) => AdjustmentDelta?.Invoke(Tones[_region], (float)((x - _startX) / Math.Max(1, _plot.ActualWidth) * (_region == 2 ? 10 : 200)));
    private void Cancel() { if (!_dragging) return; _dragging = false; _plot.ReleasePointerCaptures(); Canceled?.Invoke(); }
    private void Toggle(ClippingIndicators flag) { Clipping = _clipping ^ flag; ClippingChanged?.Invoke(Clipping); }
    private void Refresh()
    {
        _label.Text = Tones[_region]; _shadow.Selected = _clipping.HasFlag(ClippingIndicators.Shadows); _highlight.Selected = _clipping.HasFlag(ClippingIndicators.Highlights);
        _shadowMark.Text = _histogram.Luminance[0] > 0 ? "▲" : "△";
        _highlightMark.Text = _histogram.Red[255] + _histogram.Green[255] + _histogram.Blue[255] > 0 ? "▲" : "△";
        _plot.Invalidate();
    }
    private void Draw(SKCanvas canvas, Size area)
    {
        var w = (float)area.Width; var h = (float)area.Height;
        using var p = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1d1d1d") }; canvas.DrawRect(0, 0, w, h, p);
        p.Color = SKColor.Parse("#292929"); canvas.DrawRect(_region * w / 5, 0, w / 5, h, p);
        p.Color = SKColor.Parse("#343434"); p.StrokeWidth = 1; for (var i = 1; i < 5; i++) canvas.DrawLine(w * i / 5, 0, w * i / 5, h, p);
        var max = Math.Max(1, Math.Max(_histogram.Red.Max(), Math.Max(_histogram.Green.Max(), _histogram.Blue.Max())));
        int[][] channels = [_histogram.Red, _histogram.Green, _histogram.Blue]; SKColor[] colors = [new(221, 96, 98, 135), new(128, 192, 136, 135), new(105, 144, 221, 135)];
        for (var channel = 0; channel < 3; channel++)
        {
            using var path = new SKPath(); path.MoveTo(0, h);
            for (var i = 0; i < 256; i++) path.LineTo(i * w / 255, h - (float)(Math.Log(1 + channels[channel][i]) / Math.Log(1 + max)) * (h - 5));
            path.LineTo(w, h); path.Close(); p.Color = colors[channel]; p.BlendMode = SKBlendMode.Screen; canvas.DrawPath(path, p);
        }
    }
}
