using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Reusable group selection for copy, paste and synchronization. Does not own a document or clipboard.</summary>
public sealed class SettingsTransferEditor : UserControl
{
    public static IReadOnlyList<(EditSettingsGroup Group, string Label)> Entries { get; } =
    [
        (EditSettingsGroup.Light, "Light"), (EditSettingsGroup.WhiteBalance, "White balance"),
        (EditSettingsGroup.Color, "Color / B&W"), (EditSettingsGroup.Curves, "Tone curves"),
        (EditSettingsGroup.ColorMixer, "Color mixer"), (EditSettingsGroup.ColorGrading, "Color grading"),
        (EditSettingsGroup.Effects, "Effects"), (EditSettingsGroup.Detail, "Detail"),
        (EditSettingsGroup.Optics, "Optics"), (EditSettingsGroup.Geometry, "Geometry"),
        (EditSettingsGroup.Crop, "Crop / orientation"), (EditSettingsGroup.Masks, "Local masks"),
        (EditSettingsGroup.CloneSpots, "Clone spots")
    ];
    private EditSettingsGroup _groups = EditSettingsGroup.Global;
    private readonly Dictionary<EditSettingsGroup, LightButton> _buttons = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public event Action<EditSettingsGroup>? SelectionChanged;
    public EditSettingsGroup Groups
    {
        get => _groups;
        set
        {
            if ((value & ~EditSettingsGroup.All) != 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_groups == value) return;
            _groups = value; Refresh(); SelectionChanged?.Invoke(value);
        }
    }
    public SettingsTransferEditor()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var root = new StackPanel { Spacing = 10 };
        var presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        foreach (var (name, group) in new[] { ("All", EditSettingsGroup.All), ("None", EditSettingsGroup.None), ("Global", EditSettingsGroup.Global) })
        {
            var button = new LightButton("Settings " + name, text: name, action: () => Groups = group);
            presets.Children.Add(button); _widgets["settings-" + name] = button;
        }
        root.Children.Add(presets);
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 4, ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = new(1, GridUnitType.Star) } } };
        for (var i = 0; i < (Entries.Count + 1) / 2; i++) grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        for (var i = 0; i < Entries.Count; i++)
        {
            var (group, label) = Entries[i];
            var button = new LightButton("Transfer " + label, text: label, action: () => Groups ^= group)
            { HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, MinHeight = 32 };
            Grid.SetRow(button, i / 2); Grid.SetColumn(button, i % 2); grid.Children.Add(button);
            _buttons[group] = button; _widgets["group-" + group] = button;
        }
        root.Children.Add(grid);
        var note = Theme.Text("Only selected groups are replaced. Ratings, flags, captions, keywords and original files are always retained.", 11, true);
        note.TextWrapping = TextWrapping.Wrap; note.TextTrimming = TextTrimming.None; root.Children.Add(note);
        Content = root; Refresh();
    }
    private void Refresh()
    {
        foreach (var (group, label) in Entries)
            if (_buttons.TryGetValue(group, out var button))
            {
                button.Selected = (_groups & group) != 0;
                button.Text = (button.Selected ? "✓  " : "—  ") + label;
            }
    }
}
