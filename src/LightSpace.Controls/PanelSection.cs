namespace LightSpace.Controls;

/// <summary>A collapsible photography inspector section with original header chrome.</summary>
public sealed class PanelSection : StackPanel
{
    private readonly FrameworkElement _body;
    private readonly LightButton _header;
    private readonly string _title;
    public bool Expanded { get; private set; }
    public PanelSection(string title, UIElement body, bool expanded = true)
    {
        _title = title;
        _body = body as FrameworkElement ?? new Border { Child = body };
        Expanded = expanded;
        Children.Add(Theme.Divider());
        _header = new LightButton(title + " section", Glyph.None, title, Toggle)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(18, 12, 18, 12), CornerRadius = new CornerRadius(0), MinHeight = 44
        };
        Children.Add(_header); _body.Margin = new Thickness(19, 0, 19, 14); Children.Add(_body); Refresh();
    }
    public void Toggle() { Expanded = !Expanded; Refresh(); }
    private void Refresh() { _body.Visibility = Expanded ? Visibility.Visible : Visibility.Collapsed; _header.Text = (Expanded ? "−  " : "+  ") + _title; }
}
