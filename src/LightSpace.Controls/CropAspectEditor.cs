using System.Globalization;
using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable crop aspect/guide controls. The host owns crop transactions and passes live context without rebuilding this editor.</summary>
public sealed class CropAspectEditor : UserControl
{
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    private readonly Dictionary<double, LightButton> _presets = [];
    private readonly LightButton _lock, _guide, _reverse;
    private readonly TextBox _width, _height;
    private readonly TextBlock _size, _error;
    private bool _locked, _reversed;
    private CropGuide _currentGuide;
    private string _lastSize = "";
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<double>? AspectRequested;
    public event Action? OriginalRequested;
    public event Action<bool>? LockRequested;
    public event Action? SwapRequested;
    public event Action<CropGuide, bool>? GuideRequested;

    public CropAspectEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 5 };
        var heading = new Grid { ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
        heading.Children.Add(Theme.Text("Aspect ratio", 11, true));
        _lock = Button("Crop aspect lock", "Unlocked", () => LockRequested?.Invoke(!_locked));
        Grid.SetColumn(_lock, 1); heading.Children.Add(_lock); root.Children.Add(heading);
        var presets = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        for (var i = 0; i < 3; i++) presets.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        for (var i = 0; i < 2; i++) presets.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var items = new[] { ("Original", 0d), ("1 × 1", 1d), ("4 × 5", .8d), ("4 × 3", 4d / 3), ("3 × 2", 1.5d), ("16 × 9", 16d / 9) };
        for (var i = 0; i < items.Length; i++)
        {
            var (label, ratio) = items[i];
            var button = Button("Crop " + label, label, () => { if (ratio == 0) OriginalRequested?.Invoke(); else RequestAspect(ratio); });
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.Padding = new(4, 5, 4, 5);
            if (ratio > 0) _presets.Add(ratio, button);
            Grid.SetColumn(button, i % 3); Grid.SetRow(button, i / 3); presets.Children.Add(button);
        }
        root.Children.Add(presets);
        var custom = new Grid { ColumnSpacing = 4, ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto }, new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
        _width = Theme.Input("Width", "Crop ratio width", "4"); _height = Theme.Input("Height", "Crop ratio height", "5");
        _width.MinWidth = _height.MinWidth = 40; _widgets["crop-ratio-width"] = _width; _widgets["crop-ratio-height"] = _height;
        custom.Children.Add(_width); var colon = Theme.Text(":", 12); Grid.SetColumn(colon, 1); custom.Children.Add(colon);
        Grid.SetColumn(_height, 2); custom.Children.Add(_height);
        var apply = Button("Apply custom crop ratio", "Set", ApplyCustom); Grid.SetColumn(apply, 3); custom.Children.Add(apply); root.Children.Add(custom);
        _width.KeyDown += CustomKey; _height.KeyDown += CustomKey;
        _error = Theme.Text("", 10, true); _error.TextWrapping = TextWrapping.Wrap; _error.Visibility = Visibility.Collapsed; root.Children.Add(_error);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        row.Children.Add(Button("Swap crop orientation", "Swap W/H", () => SwapRequested?.Invoke()));
        _guide = Button("Cycle crop guide", "Thirds", () => GuideRequested?.Invoke((CropGuide)(((int)_currentGuide + 1) % 6), _reversed)); row.Children.Add(_guide);
        _reverse = Button("Reverse crop guide", "↔", () => GuideRequested?.Invoke(_currentGuide, !_reversed)); row.Children.Add(_reverse);
        root.Children.Add(row);
        _size = Theme.Text("", 10, true); _size.TextWrapping = TextWrapping.Wrap; root.Children.Add(_size); _widgets["crop-output-size"] = _size;
        Content = root;
    }
    private LightButton Button(string id, string text, Action action)
    {
        var button = new LightButton(id, text: text, action: action); _widgets[id] = button; return button;
    }
    private void CustomKey(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter) { ApplyCustom(); e.Handled = true; }
    }
    private void ApplyCustom()
    {
        static bool Parse(string text, out double value) => (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) && double.IsFinite(value) && value > 0;
        if (!Parse(_width.Text, out var width) || !Parse(_height.Text, out var height))
        { ShowError("Enter finite, positive width and height values."); return; }
        RequestAspect(width / height);
    }
    private void RequestAspect(double ratio)
    {
        try { AspectRequested?.Invoke(ratio); _error.Visibility = Visibility.Collapsed; }
        catch (ArgumentException error) { ShowError(error.Message); }
    }
    private void ShowError(string error) { _error.Text = error; _error.Visibility = Visibility.Visible; }
    public void SetContext(CropSettings crop, int width, int height, bool locked, CropGuide guide, bool reversed)
    {
        if (width <= 0 || height <= 0) return;
        _locked = locked; _currentGuide = guide; _reversed = reversed;
        var lockText = locked ? "Locked" : "Unlocked";
        if (_lock.Text != lockText) _lock.Text = lockText;
        if (_lock.Selected != locked) _lock.Selected = locked;
        var guideText = guide switch { CropGuide.GoldenRatio => "Golden", CropGuide.Diagonals => "Diagonal", _ => guide.ToString() };
        if (_guide.Text != guideText) _guide.Text = guideText;
        _reverse.IsEnabled = guide == CropGuide.Triangle;
        if (_reverse.Selected != reversed) _reverse.Selected = reversed;
        var ratio = CropGeometry.OutputAspect(crop, width, height); var (w, h) = crop.OutputSize(width, height);
        var size = $"{w:N0} × {h:N0} px  ·  {ratio:0.###}:1";
        if (_lastSize != size) { _lastSize = size; _size.Text = size; }
        foreach (var (preset, button) in _presets)
        {
            var selected = locked && Math.Abs(ratio - preset) < .0001;
            if (button.Selected != selected) button.Selected = selected;
        }
    }
}
