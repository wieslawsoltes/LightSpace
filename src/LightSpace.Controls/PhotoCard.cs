using LightSpace.Core;
namespace LightSpace.Controls;

/// <summary>Stable catalog card: metadata updates do not invalidate image content.</summary>
public sealed class PhotoCard : UserControl
{
    private readonly LightButton _button;
    private readonly PhotoThumbnail _thumbnail;
    private readonly TextBlock _caption;
    private readonly Border _copyBadge;
    private readonly bool _grid;
    private PhotoDocument _photo;
    private bool? _selected;
    private string? _name;
    public PhotoDocument Photo => _photo;
    public event Action<PhotoDocument>? Selected;
    public PhotoCard(PhotoDocument photo, ThumbnailCache cache, bool grid)
    {
        _photo = photo; _grid = grid;
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        _button = new LightButton("Select " + photo.Name)
        {
            Padding = new(grid ? 8 : 4), CornerRadius = new(2), BorderThickness = new(1),
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
        };
        if (!grid) { Width = 104; Height = 88; }
        var body = new Grid { RowDefinitions = { new() { Height = new(1, GridUnitType.Star) }, new() { Height = new(grid ? 25 : 17) } } };
        _thumbnail = new(photo, cache) { Height = grid ? 132 : 62, HorizontalAlignment = HorizontalAlignment.Stretch }; body.Children.Add(_thumbnail);
        _copyBadge = new Border
        {
            Child = Theme.Text("VC", 8), Background = Theme.Brush("#aa202020"), Padding = new(4, 1, 4, 1),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new(3), IsHitTestVisible = false
        };
        body.Children.Add(_copyBadge);
        _caption = Theme.Text("", grid ? 11 : 9, true); _caption.Margin = new(3, 3, 3, 0); Grid.SetRow(_caption, 1); body.Children.Add(_caption);
        _button.Content = body; _button.Click += (_, _) => Selected?.Invoke(_photo); Content = _button;
        Update(photo, false);
    }
    public void Update(PhotoDocument photo, bool selected)
    {
        _photo = photo; _thumbnail.Update(photo);
        _copyBadge.Visibility = photo.IsVirtualCopy ? Visibility.Visible : Visibility.Collapsed;
        var text = _grid ? photo.DisplayName : (photo.IsVirtualCopy ? photo.CopyName + "  " : "") + new string('★', photo.State.Rating);
        if (_caption.Text != text) _caption.Text = text;
        if (_selected != selected) { _selected = selected; _button.BorderBrush = selected ? Theme.Brush("#b4b4b4") : Theme.Line; }
        if (_name != photo.DisplayName) { _name = photo.DisplayName; ToolTipService.SetToolTip(_button, photo.DisplayName); AutomationProperties.SetName(_button, "Select " + photo.DisplayName); }
    }
}
