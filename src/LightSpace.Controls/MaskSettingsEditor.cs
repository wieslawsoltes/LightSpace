using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable local-mask parameter editor with a transaction-neutral event contract.</summary>
public sealed class MaskSettingsEditor : UserControl
{
    private LocalMask _value = new();
    private readonly ColorRangeEditor _colors = new();
    private readonly Dictionary<string, (AdjustmentSlider Slider, Func<LocalMask, float> Read)> _sliders = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    private readonly LightButton _rangeButton, _enabledButton, _invertButton;
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public LocalMask Value { get => _value; set { _value = value.Normalize(); Refresh(); } }
    public event Action<LocalMask>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public event Action? SampleColorsRequested;

    public MaskSettingsEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 3 };
        var switches = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        _enabledButton = new("Enable mask", text: "Enabled", action: () => Commit(_value with { Enabled = !_value.Enabled }));
        _invertButton = new("Invert mask", Glyph.Compare, "Invert", () => Commit(_value with { Inverted = !_value.Inverted }));
        switches.Children.Add(_enabledButton); switches.Children.Add(_invertButton); root.Children.Add(switches);
        _widgets["mask-enabled"] = _enabledButton; _widgets["Invert mask"] = _invertButton;
        Add("Amount", 0, 100, 1, m => m.Opacity * 100, (m, v) => m with { Opacity = v / 100 });
        Add("Exposure", -5, 5, .01, m => m.Exposure, (m, v) => m with { Exposure = v });
        Add("Contrast", -100, 100, 1, m => m.Contrast, (m, v) => m with { Contrast = v });
        Add("Temperature", -100, 100, 1, m => m.Temperature, (m, v) => m with { Temperature = v });
        Add("Tint", -100, 100, 1, m => m.Tint, (m, v) => m with { Tint = v });
        Add("Saturation", -100, 100, 1, m => m.Saturation, (m, v) => m with { Saturation = v });
        Add("Feather", 1, 100, 1, m => m.Feather * 100, (m, v) => m with { Feather = v / 100 });
        _rangeButton = new("Luminance restriction", text: "Restrict luminance", action: () => Commit(_value with { RangeEnabled = !_value.RangeEnabled }));
        root.Children.Add(Theme.Divider()); root.Children.Add(_rangeButton); _widgets["mask-range"] = _rangeButton;
        Add("Range minimum", 0, 100, 1, m => m.RangeMin * 100, (m, v) => m with { RangeMin = v / 100, RangeMax = Math.Max(m.RangeMax, v / 100) });
        Add("Range maximum", 0, 100, 1, m => m.RangeMax * 100, (m, v) => m with { RangeMax = v / 100, RangeMin = Math.Min(m.RangeMin, v / 100) });
        Add("Range smoothness", 1, 100, 1, m => m.RangeSmoothness * 100, (m, v) => m with { RangeSmoothness = v / 100 });
        root.Children.Add(Theme.Divider());
        Add("Angle", 0, 359, 1, m => m.Angle, (m, v) => m with { Angle = v });
        Add("Horizontal position", 0, 100, 1, m => m.X * 100, (m, v) => m with { X = v / 100 });
        Add("Vertical position", 0, 100, 1, m => m.Y * 100, (m, v) => m with { Y = v / 100 });
        root.Children.Insert(2, _colors);
        _colors.Previewed += value => { _value = _value with { ColorRange = value }; Previewed?.Invoke(_value); };
        _colors.Committed += () => Committed?.Invoke(); _colors.Canceled += () => Canceled?.Invoke();
        _colors.SampleRequested += () => SampleColorsRequested?.Invoke();
        foreach (var (id, widget) in _colors.Widgets) _widgets[id] = widget;
        Content = root; Refresh();
        void Add(string name, double minimum, double maximum, double step, Func<LocalMask, float> read, Func<LocalMask, float, LocalMask> edit)
        {
            var slider = new AdjustmentSlider(name, minimum, maximum, minimum > 0 ? minimum : 0, step);
            slider.ValueChanged += value => { _value = edit(_value, value).Normalize(); Refresh(); Previewed?.Invoke(_value); };
            slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke();
            _sliders[name] = (slider, read); _widgets["mask-" + name] = slider.TrackElement; root.Children.Add(slider);
        }
    }
    private void Commit(LocalMask value) { _value = value.Normalize(); Refresh(); Previewed?.Invoke(_value); Committed?.Invoke(); }
    private void Refresh()
    {
        _colors.Value = _value.ColorRange; _colors.Required = _value.Kind == MaskKind.ColorRange;
        foreach (var (slider, read) in _sliders.Values) slider.Value = read(_value);
        _enabledButton.Selected = _value.Enabled; _enabledButton.Text = _value.Enabled ? "Enabled" : "Disabled";
        _invertButton.Selected = _value.Inverted;
        var ranged = _value.RangeEnabled || _value.Kind == MaskKind.LuminanceRange;
        _rangeButton.Selected = ranged; _rangeButton.IsEnabled = _value.Kind != MaskKind.LuminanceRange;
        foreach (var name in new[] { "Range minimum", "Range maximum", "Range smoothness" }) _sliders[name].Slider.IsEnabled = ranged;
        foreach (var name in new[] { "Angle", "Horizontal position", "Vertical position" }) _sliders[name].Slider.Visibility = _value.Kind is MaskKind.LuminanceRange or MaskKind.Brush or MaskKind.ColorRange ? Visibility.Collapsed : Visibility.Visible;
        _sliders["Feather"].Slider.Visibility = _value.Kind == MaskKind.Radial ? Visibility.Visible : Visibility.Collapsed;
    }
}
