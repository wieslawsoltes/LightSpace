using LightSpace.Core;
namespace LightSpace.Controls;

public sealed class ColorMixerEditor : UserControl
{
    private ColorBand[] _value = DevelopSettings.Default.Mixer;
    private int _band;
    private readonly AdjustmentSlider _hue = new("Hue"), _saturation = new("Saturation"), _luminance = new("Luminance");
    private readonly TextBlock _name = Theme.Text("", 11, true);
    private readonly List<LightButton> _buttons = [];
    private static readonly string[] Names = ["Red", "Orange", "Yellow", "Green", "Aqua", "Blue", "Purple", "Magenta"];
    public ColorBand[] Value { get => _value; set { if (value.Length != 8) throw new ArgumentException("Eight bands required."); _value = value; Refresh(); } }
    public event Action<ColorBand[]>? Previewed;
    public event Action? Committed;
    public event Action? Canceled;
    public ColorMixerEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 6 }; var bands = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        string[] colors = ["#bc6262", "#d69b60", "#c9b66b", "#70a775", "#6bafa9", "#6b8cbd", "#9277b6", "#b374a7"];
        for (var i = 0; i < 8; i++)
        {
            var band = i; var button = new LightButton(Names[i] + " mixer", action: () => { Committed?.Invoke(); _band = band; Refresh(); })
            { Width = 28, MinWidth = 28, Padding = new(5), Content = new Border { Width = 16, Height = 16, CornerRadius = new(8), Background = Theme.Brush(colors[i]) } };
            bands.Children.Add(button); _buttons.Add(button);
        }
        root.Children.Add(bands); root.Children.Add(_name); root.Children.Add(_hue); root.Children.Add(_saturation); root.Children.Add(_luminance); Content = root;
        _hue.ValueChanged += value => Edit(_value[_band] with { Hue = value });
        _saturation.ValueChanged += value => Edit(_value[_band] with { Saturation = value });
        _luminance.ValueChanged += value => Edit(_value[_band] with { Luminance = value });
        foreach (var slider in new[] { _hue, _saturation, _luminance }) { slider.ValueCommitted += () => Committed?.Invoke(); slider.GestureCanceled += () => Canceled?.Invoke(); }
        Refresh();
    }
    private void Edit(ColorBand band) { var next = (ColorBand[])_value.Clone(); next[_band] = band; _value = next; Previewed?.Invoke(next); }
    private void Refresh()
    {
        _name.Text = Names[_band]; var band = _value[_band]; _hue.Value = band.Hue; _saturation.Value = band.Saturation; _luminance.Value = band.Luminance;
        for (var i = 0; i < _buttons.Count; i++) _buttons[i].Selected = i == _band;
    }
}
