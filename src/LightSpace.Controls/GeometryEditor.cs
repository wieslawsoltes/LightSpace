using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable projective framing controls; transaction ownership belongs to the embedding editor.</summary>
public sealed class GeometryEditor : UserControl
{
    private GeometrySettings _value = new();
    private readonly Dictionary<string, AdjustmentSlider> _sliders = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    private readonly LightButton _constrain;
    private bool _compact;
    public bool Compact { get => _compact; set { _compact = value; foreach (var (name, slider) in _sliders) slider.Visibility = !value || name == "Rotate" ? Visibility.Visible : Visibility.Collapsed; } }
    public GeometrySettings Value { get => _value; set { if (_value == value) return; _value = value.Normalize(); Refresh(); } }
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<GeometrySettings>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public event Action? StraightenRequested;
    public GeometryEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 2 };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        var straighten = new LightButton("Straighten horizon", Glyph.Rotate, "Straighten", () => StraightenRequested?.Invoke());
        var reset = new LightButton("Reset geometry", Glyph.Undo, "Reset", () => Commit(new()));
        actions.Children.Add(straighten); actions.Children.Add(reset); root.Children.Add(actions);
        _widgets["Straighten horizon"] = straighten; _widgets["Reset geometry"] = reset;
        foreach (var (name, label, min, max, initial, step) in new[]
        {
            ("Vertical", "Vertical", -100d, 100d, 0d, 1d), ("Horizontal", "Horizontal", -100d, 100d, 0d, 1d),
            ("Rotate", "Rotate", -45d, 45d, 0d, .1d), ("Aspect", "Aspect", -100d, 100d, 0d, 1d),
            ("Scale", "Scale", 50d, 200d, 100d, 1d), ("XOffset", "X Offset", -100d, 100d, 0d, 1d),
            ("YOffset", "Y Offset", -100d, 100d, 0d, 1d)
        })
        {
            var slider = new AdjustmentSlider(label, min, max, initial, step);
            slider.ValueChanged += v => { _value = _value.Set(name, v); Previewed?.Invoke(_value); };
            slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke();
            _sliders[name] = slider; _widgets["geometry-" + name] = slider.TrackElement; root.Children.Add(slider);
        }
        _constrain = new("Constrain crop", Glyph.Crop, "Constrain crop", () => Commit(_value with { ConstrainCrop = !_value.ConstrainCrop }));
        root.Children.Add(_constrain); _widgets["Constrain crop"] = _constrain;
        Content = root; Refresh();
    }
    private void Commit(GeometrySettings settings) { _value = settings; Refresh(); Previewed?.Invoke(_value); Committed?.Invoke(); }
    private void Refresh()
    {
        foreach (var (name, slider) in _sliders) slider.Value = _value.Get(name);
        _constrain.Selected = _value.ConstrainCrop;
    }
}
