namespace LightSpace.Controls;

public sealed class PanelSection : StackPanel
{
    private readonly FrameworkElement _body;
    private readonly string _title;
    public LightButton Header { get; }
    public bool Expanded { get; private set; }
    public event Action<bool>? ExpandedChanged;
    public PanelSection(string title, UIElement body, bool expanded = true)
    {
        _title = title; _body = body as FrameworkElement ?? new Border { Child = body }; Expanded = expanded;
        Children.Add(Theme.Divider());
        Header = new LightButton(title + " section", Glyph.None, title, Toggle)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new(18, 12, 18, 12), CornerRadius = new(0), MinHeight = 44
        };
        Children.Add(Header); _body.Margin = new(19, 0, 19, 14); Children.Add(_body); Refresh();
    }
    public void Toggle() { Expanded = !Expanded; Refresh(); ExpandedChanged?.Invoke(Expanded); }
    private void Refresh() { _body.Visibility = Expanded ? Visibility.Visible : Visibility.Collapsed; Header.Text = (Expanded ? "−  " : "+  ") + _title; }
}
