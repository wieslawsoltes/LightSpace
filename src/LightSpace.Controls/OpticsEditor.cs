using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Manual distortion, channel alignment and light-falloff controls. No proprietary lens profiles are implied.</summary>
public sealed class OpticsEditor : UserControl
{
    private LensCorrectionSettings _value = new();
    private readonly Dictionary<string, AdjustmentSlider> _sliders = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    public LensCorrectionSettings Value { get => _value; set { if (_value == value) return; _value = value.Normalize(); Refresh(); } }
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<LensCorrectionSettings>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public OpticsEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 3 };
        var label = Theme.Text("MANUAL CORRECTIONS", 10, true); label.Margin = new(0, 5, 0, 10); root.Children.Add(label);
        foreach (var (name, title) in new[] { ("Distortion", "Distortion"), ("Vignetting", "Lens vignetting"), ("RedCyan", "Red / Cyan"), ("BlueYellow", "Blue / Yellow") })
        {
            var slider = new AdjustmentSlider(title);
            slider.ValueChanged += v => { _value = _value.Set(name, v); Previewed?.Invoke(_value); };
            slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke();
            root.Children.Add(slider); _sliders[name] = slider; _widgets["optics-" + name] = slider.TrackElement;
        }
        var reset = new LightButton("Reset optics", Glyph.Undo, "Reset optics", () =>
        { _value = new(); Refresh(); Previewed?.Invoke(_value); Committed?.Invoke(); });
        _widgets["Reset optics"] = reset; root.Children.Add(reset);
        var note = Theme.Text("Manual optical model. No camera or lens-profile database is applied.", 10, true);
        note.TextWrapping = TextWrapping.Wrap; note.Margin = new(0, 8, 0, 6); root.Children.Add(note); Content = root;
    }
    private void Refresh() { foreach (var (name, slider) in _sliders) slider.Value = _value.Get(name); }
}
