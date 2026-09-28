using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Five-sample color-selection editor. The owner supplies source picking and transaction boundaries.</summary>
public sealed class ColorRangeEditor : UserControl
{
    private ColorRangeSettings _value = new();
    private bool _required;
    private readonly LightButton _enabled, _sample;
    private readonly AdjustmentSlider _tolerance, _smoothness;
    private readonly LightButton[] _swatches = new LightButton[5];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<ColorRangeSettings>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public event Action? SampleRequested;
    public ColorRangeSettings Value { get => _value; set { _value = value.Normalize(); Refresh(); } }
    public bool Required { get => _required; set { _required = value; Refresh(); } }
    public ColorRangeEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 4 };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        _enabled = new("Restrict color", text: "Restrict color", action: () => Commit(_value with { Enabled = !_value.Enabled }));
        _sample = new("Sample colors", Glyph.Search, "Sample", () => SampleRequested?.Invoke());
        actions.Children.Add(_enabled); actions.Children.Add(_sample); root.Children.Add(actions);
        _widgets["Restrict color"] = _enabled; _widgets["Sample colors"] = _sample;
        var samples = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new(2, 0, 2, 0) };
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            var button = new LightButton("Remove color sample " + (i + 1), action: () =>
            {
                if (index < _value.Samples.Length) Commit(_value with { Samples = _value.Samples.Where((_, j) => index != j).ToArray() });
            }) { Width = 30, Height = 26, MinWidth = 26, MinHeight = 26, Padding = new(3) };
            _swatches[i] = button; samples.Children.Add(button); _widgets["color-swatch-" + i] = button;
        }
        root.Children.Add(samples);
        _tolerance = new("Color tolerance", 0, 100, 8, .1);
        _smoothness = new("Color smoothness", .1, 100, 6, .1);
        _tolerance.ValueChanged += v => Preview(_value with { Tolerance = v / 100 });
        _smoothness.ValueChanged += v => Preview(_value with { Smoothness = v / 100 });
        foreach (var slider in new[] { _tolerance, _smoothness })
        {
            slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke(); root.Children.Add(slider);
        }
        _widgets["color-tolerance"] = _tolerance.TrackElement; _widgets["color-smoothness"] = _smoothness.TrackElement;
        Content = root; Refresh();
    }
    private void Preview(ColorRangeSettings settings) { _value = settings.Normalize(); Refresh(); Previewed?.Invoke(_value); }
    private void Commit(ColorRangeSettings settings) { Preview(settings); Committed?.Invoke(); }
    private void Refresh()
    {
        var active = _required || _value.Enabled;
        _enabled.Selected = active; _enabled.IsEnabled = !_required;
        _tolerance.Value = _value.Tolerance * 100; _smoothness.Value = _value.Smoothness * 100;
        _tolerance.Visibility = _smoothness.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        for (var i = 0; i < _swatches.Length; i++)
        {
            var button = _swatches[i]; var sample = i < _value.Samples.Length ? _value.Samples[i] : null;
            button.Visibility = sample is null ? Visibility.Collapsed : Visibility.Visible;
            if (Equals(button.Tag, sample)) continue;
            button.Tag = sample;
            if (sample is not null)
            {
                var color = new SKColor((byte)MathF.Round(sample.Red * 255), (byte)MathF.Round(sample.Green * 255), (byte)MathF.Round(sample.Blue * 255));
                button.Content = new Border { Background = Theme.Brush(color.ToString()), CornerRadius = new(3), BorderBrush = Theme.Line, BorderThickness = new(1) };
                ToolTipService.SetToolTip(button, $"Remove sample {i + 1}: RGB {color.Red}, {color.Green}, {color.Blue}");
            }
        }
    }
}
