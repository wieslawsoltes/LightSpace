using Microsoft.UI.Xaml.Markup;
namespace LightSpace.Controls;

/// <summary>Original button chrome with a separate keyboard-focus overlay.</summary>
public sealed class LightButton : Button
{
    private readonly TextBlock? _label;
    private readonly IconView? _icon;
    private Border? _focusRing;
    private bool _selected, _hover;
    private static ControlTemplate? _template;
    public bool Selected { get => _selected; set { if (_selected == value) return; _selected = value; Refresh(); } }
    public string Text { get => _label?.Text ?? ""; set { if (_label is not null && _label.Text != value) _label.Text = value; } }
    public LightButton(string name, Glyph glyph = Glyph.None, string? text = null, Action? action = null)
    {
        _template ??= (ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Button">
              <Grid>
                <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="{TemplateBinding CornerRadius}" Padding="{TemplateBinding Padding}">
                  <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="{TemplateBinding VerticalContentAlignment}" />
                </Border>
                <Border x:Name="KeyboardFocusRing" BorderBrush="#91c3ef" BorderThickness="1" CornerRadius="{TemplateBinding CornerRadius}" IsHitTestVisible="False" Visibility="Collapsed" />
              </Grid>
            </ControlTemplate>
            """);
        Template = _template; FontFamily = Theme.Font; FontSize = 12; Foreground = Theme.TextColor;
        Padding = new(10, 7, 10, 7); CornerRadius = new(4); BorderThickness = new(1); BorderBrush = Theme.Brush("#00000000"); MinHeight = 32; MinWidth = 32;
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        if (glyph != Glyph.None) { _icon = new(glyph); content.Children.Add(_icon); }
        if (text is not null) { _label = Theme.Text(text); content.Children.Add(_label); }
        Content = content; AutomationProperties.SetName(this, name); AutomationProperties.SetAutomationId(this, name); ToolTipService.SetToolTip(this, name);
        PointerEntered += (_, _) => { _hover = true; Refresh(); }; PointerExited += (_, _) => { _hover = false; Refresh(); };
        // Focus must not overwrite a card's persistent selection border.
        GotFocus += (_, _) => RefreshFocus(); LostFocus += (_, _) => RefreshFocus();
        if (action is not null) Click += (_, _) => action(); Refresh();
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate(); _focusRing = GetTemplateChild("KeyboardFocusRing") as Border; RefreshFocus();
    }
    private void RefreshFocus()
    {
        if (_focusRing is not null) _focusRing.Visibility = FocusState == FocusState.Keyboard ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Refresh()
    {
        Background = Theme.Brush(_selected ? "#3b3b3b" : _hover ? "#303030" : "#00000000");
        if (_icon is not null) _icon.Color = SKColor.Parse(_selected ? "#91c3ef" : "#c4c4c4");
    }
}
