using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Independent four-range color-grading control. The host supplies transaction and persistence behavior.</summary>
public sealed class ColorGradingEditor : UserControl
{
    private ColorGradingSettings _value = new();
    private readonly ColorWheel _wheel = new();
    private readonly AdjustmentSlider _hue = new("Hue", 0, 359, 0, 1);
    private readonly AdjustmentSlider _saturation = new("Saturation", 0, 100);
    private readonly AdjustmentSlider _luminance = new("Luminance");
    private readonly AdjustmentSlider _blending = new("Blending", 0, 100, 50);
    private readonly AdjustmentSlider _balance = new("Balance");
    private readonly Dictionary<GradingRange, LightButton> _ranges = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public GradingRange ActiveRange { get; private set; } = GradingRange.Midtones;
    public ColorGradingSettings Value { get => _value; set { _value = value.Normalize(); Refresh(); } }
    public event Action<ColorGradingSettings>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;

    public ColorGradingEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var stack = new StackPanel { Spacing = 4 };
        var tabs = new Grid();
        string[] names = ["Shadows", "Mids", "Highs", "Global"];
        for (var i = 0; i < 4; i++)
        {
            var range = (GradingRange)i; tabs.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
            var button = new LightButton("Grade " + range, text: names[i], action: () => { Committed?.Invoke(); ActiveRange = range; Refresh(); })
            { Padding = new(3, 7, 3, 7), MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
            Grid.SetColumn(button, i); tabs.Children.Add(button); _ranges[range] = button; _widgets["grading-" + range] = button;
        }
        stack.Children.Add(tabs); stack.Children.Add(_wheel); _widgets["grading-wheel"] = _wheel;
        _wheel.ValueChanged += tone => Edit(_value.Set(ActiveRange, tone)); _wheel.Committed += () => Committed?.Invoke(); _wheel.Canceled += () => Canceled?.Invoke();
        Add(_hue, "Hue", value => Edit(_value.Set(ActiveRange, _value.Get(ActiveRange) with { Hue = value })));
        Add(_saturation, "Saturation", value => Edit(_value.Set(ActiveRange, _value.Get(ActiveRange) with { Saturation = value })));
        Add(_luminance, "Luminance", value => Edit(_value.Set(ActiveRange, _value.Get(ActiveRange) with { Luminance = value })));
        stack.Children.Add(Theme.Divider());
        Add(_blending, "Blending", value => Edit(_value with { Blending = value }));
        Add(_balance, "Balance", value => Edit(_value with { Balance = value }));
        var reset = new LightButton("Reset grading range", Glyph.Undo, "Reset range", () => { Edit(_value.Set(ActiveRange, new())); Committed?.Invoke(); });
        stack.Children.Add(reset); _widgets["grading-reset"] = reset;
        Content = stack; Refresh();
        void Add(AdjustmentSlider slider, string name, Action<float> changed)
        {
            slider.ValueChanged += changed; slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke();
            stack.Children.Add(slider); _widgets["grading-" + name] = slider.TrackElement;
        }
    }
    private void Edit(ColorGradingSettings value) { _value = value.Normalize(); Refresh(); Previewed?.Invoke(_value); }
    private void Refresh()
    {
        var tone = _value.Get(ActiveRange); _wheel.Value = tone;
        _hue.Value = tone.Hue; _saturation.Value = tone.Saturation; _luminance.Value = tone.Luminance;
        _blending.Value = _value.Blending; _balance.Value = _value.Balance;
        foreach (var (range, button) in _ranges) button.Selected = range == ActiveRange;
    }
}
