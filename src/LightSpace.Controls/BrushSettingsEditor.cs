using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Tool settings are separate from immutable strokes; changing the brush does not rewrite earlier paint.</summary>
public sealed class BrushSettingsEditor : UserControl
{
    private BrushStroke _value = new();
    private readonly AdjustmentSlider _size, _feather, _flow, _density;
    private readonly LightButton _paint, _erase;
    public BrushStroke Value
    {
        get => _value;
        set { _value = value.Normalize(); Refresh(); }
    }
    public event Action<BrushStroke>? Changed;
    public BrushSettingsEditor()
    {
        var root = new StackPanel { Spacing = 4 };
        var modes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        _paint = new("Paint brush", text: "Paint", action: () => Set(_value with { Erase = false }));
        _erase = new("Erase brush", text: "Erase", action: () => Set(_value with { Erase = true }));
        modes.Children.Add(_paint); modes.Children.Add(_erase); root.Children.Add(modes);
        _size = new("Brush size", .2, 25, 4, .1);
        _feather = new("Brush feather", 0, 100, 70);
        _flow = new("Brush flow", 1, 100, 35);
        _density = new("Brush density", 1, 100, 100);
        _size.ValueChanged += v => Set(_value with { Radius = v / 100 });
        _feather.ValueChanged += v => Set(_value with { Feather = v / 100 });
        _flow.ValueChanged += v => Set(_value with { Flow = v / 100 });
        _density.ValueChanged += v => Set(_value with { Density = v / 100 });
        foreach (var slider in new[] { _size, _feather, _flow, _density }) root.Children.Add(slider);
        Content = root; Refresh();
    }
    private void Set(BrushStroke value) { _value = value.Normalize(); Refresh(); Changed?.Invoke(_value); }
    private void Refresh()
    {
        _size.Value = _value.Radius * 100; _feather.Value = _value.Feather * 100;
        _flow.Value = _value.Flow * 100; _density.Value = _value.Density * 100;
        _paint.Selected = !_value.Erase; _erase.Selected = _value.Erase;
    }
}
