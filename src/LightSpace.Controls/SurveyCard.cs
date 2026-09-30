using LightSpace.Core;
using Microsoft.UI.Xaml.Markup;
namespace LightSpace.Controls;

/// <summary>Native Uno focus/input/accessibility chrome over a shared Skia photo surface.</summary>
public sealed class SurveyCard : UserControl
{
    private sealed class PhotoTarget(SurveyCard owner) : Button
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) { owner.OpenRequested?.Invoke(owner._photo.Id); e.Handled = true; }
            else base.OnKeyDown(e);
        }
    }
    private static ControlTemplate? _hitTemplate;
    private readonly Button _hit;
    private readonly TextBlock _name, _pending;
    private readonly LightButton _rating, _pick, _reject, _exclude;
    private readonly Grid _root;
    private PhotoDocument _photo;
    private bool _active, _hover;
    public FrameworkElement HitTarget => _hit;
    public IReadOnlyList<FrameworkElement> Actions => [_rating, _pick, _reject, _exclude];
    public event Action<Guid>? Activated;
    public event Action<Guid>? OpenRequested;
    public event Action<Guid>? RatingRequested;
    public event Action<Guid, PhotoFlag>? FlagRequested;
    public event Action<Guid>? ExcludeRequested;

    public SurveyCard(PhotoDocument photo)
    {
        _photo = photo;
        SizeChanged += (_, _) => Clip = new RectangleGeometry { Rect = new Rect(0, 0, Math.Max(0, ActualWidth), Math.Max(0, ActualHeight)) };
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        _hitTemplate ??= (ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Button">
              <Border Background="Transparent" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="Center" VerticalAlignment="Center" />
              </Border>
            </ControlTemplate>
            """);
        _pending = Theme.Text("Preparing preview…", 10, true);
        _hit = new PhotoTarget(this) { Template = _hitTemplate, Content = _pending, BorderThickness = new(2), Padding = new(0), MinWidth = 0, MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        _hit.Click += (_, _) => Activated?.Invoke(_photo.Id);
        _hit.DoubleTapped += (_, e) => { OpenRequested?.Invoke(_photo.Id); e.Handled = true; };
        _hit.PointerEntered += (_, _) => { _hover = true; Border(); };
        _hit.PointerExited += (_, _) => { _hover = false; Border(); };
        _hit.GotFocus += (_, _) => Border(); _hit.LostFocus += (_, _) => Border();
        _name = Theme.Text(photo.DisplayName, 10, true); _name.Margin = new(4, 0, 4, 0);
        _root = new Grid { RowDefinitions = { new() { Height = new(1, GridUnitType.Star) }, new() { Height = new(21) }, new() { Height = new(27) } } };
        _root.Children.Add(_hit); Grid.SetRow(_name, 1); _root.Children.Add(_name);
        var actions = new Grid { ColumnSpacing = 1 };
        for (var i = 0; i < 4; i++) actions.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        _rating = new("Cycle rating", text: "0★", action: () => RatingRequested?.Invoke(_photo.Id));
        _pick = new("Pick this photo", Glyph.Flag, action: () => FlagRequested?.Invoke(_photo.Id, PhotoFlag.Pick));
        _reject = new("Reject this photo", Glyph.RejectFlag, action: () => FlagRequested?.Invoke(_photo.Id, PhotoFlag.Reject));
        _exclude = new("Exclude from survey, not from catalog", Glyph.Close, action: () => ExcludeRequested?.Invoke(_photo.Id));
        LightButton[] buttons = [_rating, _pick, _reject, _exclude];
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i]; button.HorizontalAlignment = HorizontalAlignment.Stretch; button.VerticalAlignment = VerticalAlignment.Stretch; button.MinWidth = 0; button.MinHeight = 0; button.Padding = new(1, 0, 1, 0);
            Grid.SetColumn(button, i); actions.Children.Add(button);
        }
        Grid.SetRow(actions, 2); _root.Children.Add(actions); Content = _root; Update(photo, false);
    }
    public void Update(PhotoDocument photo, bool active)
    {
        _photo = photo; _active = active;
        if (_name.Text != photo.DisplayName) _name.Text = photo.DisplayName;
        var rating = photo.State.Rating + "★";
        if (_rating.Text != rating) _rating.Text = rating;
        _pick.Selected = photo.State.Flag == PhotoFlag.Pick; _reject.Selected = photo.State.Flag == PhotoFlag.Reject;
        AutomationProperties.SetName(_hit, $"{photo.DisplayName}, {photo.State.Rating} stars, {photo.State.Flag}");
        ToolTipService.SetToolTip(_name, photo.DisplayName); Border();
    }
    public void SetReady(bool ready, bool failed = false)
    {
        _pending.Text = failed ? "Preview unavailable" : "Preparing preview…";
        _pending.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
    }
    public void SetFooter(double availableHeight)
    {
        _root.RowDefinitions[1].Height = new(Math.Min(21, availableHeight * 21 / 48));
        _root.RowDefinitions[2].Height = new(Math.Min(27, availableHeight * 27 / 48));
    }
    private void Border() => _hit.BorderBrush = _active || _hit.FocusState == FocusState.Keyboard ? Theme.Accent : _hover ? Theme.TextColor : Theme.Brush("#3a3a3a");
}
