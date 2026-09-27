namespace LightSpace.Controls;

public static class Theme
{
    public static FontFamily Font { get; set; } = new("Segoe UI");
    public static SolidColorBrush Brush(string color) { var c = SKColor.Parse(color); return new(Windows.UI.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue)); }
    public static SolidColorBrush Background => Brush("#1b1b1b");
    public static SolidColorBrush Panel => Brush("#242424");
    public static SolidColorBrush Sidebar => Brush("#202020");
    public static SolidColorBrush Line => Brush("#393939");
    public static SolidColorBrush TextColor => Brush("#dedede");
    public static SolidColorBrush Muted => Brush("#999999");
    public static SolidColorBrush Accent => Brush("#78acdf");
    public static TextBlock Text(string text, double size = 12, bool muted = false) => new()
    {
        Text = text, FontSize = size, FontFamily = Font, Foreground = muted ? Muted : TextColor,
        VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
    };
    public static Border Divider() => new() { Height = 1, Background = Line, HorizontalAlignment = HorizontalAlignment.Stretch };
    public static TextBox Input(string placeholder, string name, string value = "")
    {
        var box = new TextBox { PlaceholderText = placeholder, Text = value, FontFamily = Font, FontSize = 12, Background = Brush("#191919"), Foreground = TextColor, BorderBrush = Line, BorderThickness = new(1), CornerRadius = new(4), Padding = new(10, 7, 10, 7), MinWidth = 0, MinHeight = 30 };
        AutomationProperties.SetName(box, name); AutomationProperties.SetAutomationId(box, name); return box;
    }
}
