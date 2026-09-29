using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

/// <summary>
/// Read-only photographic reference, drawn directly through the shared Skia
/// renderer. Owns no document history or renderer; the host controls lifetime.
/// </summary>
public sealed class ReferencePhotoView : UserControl
{
    private sealed class Surface(ReferencePhotoView owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Render(canvas, area);
    }
    private readonly PhotoRenderer _renderer;
    private readonly Surface _surface;
    private PhotoDocument? _photo;
    private PhotoNavigationState _navigation = PhotoNavigationState.Fit;
    private PhotoNavigationState _start;
    private Point _press;
    private bool _dragging;
    private string? _lastError;
    public event Action<PhotoNavigationState>? NavigationChanged;
    public event Action<string>? RenderFailed;
    public PhotoDocument? Photo
    {
        get => _photo;
        set { if (ReferenceEquals(value, _photo)) return; Cancel(); _photo = value; _lastError = null; SetNavigation(PhotoNavigationState.Fit); _surface.Invalidate(); }
    }
    public PhotoNavigationState Navigation => _navigation;
    public long RenderCount { get; private set; }
    public Rect ImageBounds { get; private set; }
    public ReferencePhotoView(PhotoRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer); _renderer = renderer;
        _surface = new(this); Content = _surface; IsTabStop = true; Background = Theme.Background;
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        AutomationProperties.SetName(this, "Reference photograph");
        _surface.PointerPressed += (_, e) =>
        {
            if (_photo is null || !e.GetCurrentPoint(_surface).Properties.IsLeftButtonPressed) return;
            Focus(FocusState.Pointer); _press = e.GetCurrentPoint(_surface).Position; _start = _navigation;
            _dragging = _surface.CapturePointer(e.Pointer); e.Handled = true;
        };
        _surface.PointerMoved += (_, e) =>
        {
            if (!_dragging) return; var point = e.GetCurrentPoint(_surface).Position;
            Navigate(_start with { PanX = _start.PanX + (float)((point.X - _press.X) / Math.Max(1, ActualWidth)), PanY = _start.PanY + (float)((point.Y - _press.Y) / Math.Max(1, ActualHeight)) }); e.Handled = true;
        };
        _surface.PointerReleased += (_, e) => { if (!_dragging) return; _dragging = false; _surface.ReleasePointerCapture(e.Pointer); e.Handled = true; };
        _surface.PointerCanceled += (_, _) => Cancel(); _surface.PointerCaptureLost += (_, _) => Cancel();
        _surface.PointerWheelChanged += (_, e) =>
        {
            if (_dragging || _photo is null) return;
            var point = e.GetCurrentPoint(_surface); var old = _navigation.Zoom;
            var zoom = Math.Clamp(old * (point.Properties.MouseWheelDelta > 0 ? 1.15f : 1 / 1.15f), .01f, 64);
            var x = (float)(point.Position.X / Math.Max(1, ActualWidth)); var y = (float)(point.Position.Y / Math.Max(1, ActualHeight));
            Navigate(new(zoom, (_navigation.PanX + .5f - x) * zoom / old + x - .5f, (_navigation.PanY + .5f - y) * zoom / old + y - .5f)); e.Handled = true;
        };
        _surface.DoubleTapped += (_, e) => { ToggleZoom(); e.Handled = true; };
        KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape && _dragging) { Cancel(); e.Handled = true; }
            else if (e.Key == VirtualKey.Home) { Fit(); e.Handled = true; }
            else if (e.Key == VirtualKey.Z) { ToggleZoom(); e.Handled = true; }
        };
        SizeChanged += (_, _) => { SetNavigation(_navigation); _surface.Invalidate(); };
    }
    private void Cancel()
    {
        if (!_dragging) return; _dragging = false; _surface.ReleasePointerCaptures(); Navigate(_start);
    }
    public void Fit() => Navigate(PhotoNavigationState.Fit);
    public void ToggleZoom()
    {
        if (_photo is null) return;
        var (w, h) = _photo.State.Crop.OutputSize(_photo.Width, _photo.Height);
        var fit = Math.Min(Math.Max(1, (float)ActualWidth - 52) / w, Math.Max(1, (float)ActualHeight - 48) / h);
        var zoom = Math.Abs(_navigation.Zoom - 1) < .001f ? Math.Clamp(1 / (fit * (float)(XamlRoot?.RasterizationScale ?? 1)), .01f, 64) : 1;
        Navigate(new(zoom));
    }
    private void Navigate(PhotoNavigationState value) { SetNavigation(value); NavigationChanged?.Invoke(_navigation); }
    public void SetNavigation(PhotoNavigationState value)
    {
        value = value.Normalize();
        if (_photo is not null && ActualWidth > 0 && ActualHeight > 0)
        {
            var (w, h) = _photo.State.Crop.OutputSize(_photo.Width, _photo.Height);
            var aw = Math.Max(1, (float)ActualWidth - 52); var ah = Math.Max(1, (float)ActualHeight - 48);
            var scale = Math.Min(aw / w, ah / h) * value.Zoom;
            var x = Math.Max(0, (aw + w * scale) / 2 - 32) / (float)ActualWidth;
            var y = Math.Max(0, (ah + h * scale) / 2 - 32) / (float)ActualHeight;
            value = value with { PanX = Math.Clamp(value.PanX, -x, x), PanY = Math.Clamp(value.PanY, -y, y) };
        }
        if (_navigation == value) return; _navigation = value; _surface.Invalidate();
    }
    private void Render(SKCanvas canvas, Size area)
    {
        canvas.Clear(SKColor.Parse("#171717")); if (_photo is null) return;
        try
        {
            var available = SKRect.Create(26, 24, Math.Max(1, (float)area.Width - 52), Math.Max(1, (float)area.Height - 48));
            var bounds = PhotoTransform.Fit(_photo.State.Crop, _photo.Width, _photo.Height, available, _navigation.Zoom, _navigation.PanX * (float)area.Width, _navigation.PanY * (float)area.Height);
            ImageBounds = new(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            _renderer.Draw(canvas, _photo, bounds); RenderCount++;
        }
        catch (Exception error) { if (_lastError != error.Message) { _lastError = error.Message; RenderFailed?.Invoke(error.Message); } }
    }
}
