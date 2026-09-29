using LightSpace.Core;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private Action<PhotoNavigationState>? _navigationChanged;
    private PhotoNavigationState _publishedNavigation = PhotoNavigationState.Fit;
    public PhotoNavigationState Navigation => new(Zoom, PanX / Math.Max(1, (float)ActualWidth), PanY / Math.Max(1, (float)ActualHeight));
    public event Action<PhotoNavigationState> NavigationChanged
    {
        add
        {
            if (_navigationChanged is null) { ViewChanged += PublishNavigation; _surface.PointerMoved += TrackNavigation; }
            _navigationChanged += value;
        }
        remove
        {
            _navigationChanged -= value;
            if (_navigationChanged is null) { ViewChanged -= PublishNavigation; _surface.PointerMoved -= TrackNavigation; }
        }
    }
    private void TrackNavigation(object sender, PointerRoutedEventArgs e)
    {
        if (_dragging && Tool == PhotoTool.Edit && !_dragComparison) PublishNavigation();
    }
    private void PublishNavigation()
    {
        var navigation = Navigation;
        if (navigation == _publishedNavigation) return;
        _publishedNavigation = navigation; _navigationChanged?.Invoke(navigation);
    }
    /// <summary>Apply view-only linked navigation without editing state or generating a feedback event.</summary>
    public void SetNavigation(PhotoNavigationState navigation)
    {
        navigation = navigation.Normalize();
        if (Navigation == navigation) return;
        Zoom = navigation.Zoom; PanX = navigation.PanX * (float)ActualWidth; PanY = navigation.PanY * (float)ActualHeight;
        ConstrainPan(); _publishedNavigation = Navigation; Invalidate();
    }
}
