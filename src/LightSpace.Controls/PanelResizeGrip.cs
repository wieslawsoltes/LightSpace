namespace LightSpace.Controls;

/// <summary>Keyboard-accessible panel width grip. Delta is measured in a stable parent coordinate space.</summary>
public sealed class PanelResizeGrip : UserControl
{
    private readonly Border _hit = new() { Background = Theme.Brush("#00000000") };
    private bool _dragging;
    private double _start;
    public event Action? Started;
    public event Action<double>? Delta;
    public event Action? Completed;
    public event Action? Canceled;
    public event Action? Reset;
    public PanelResizeGrip(string name)
    {
        Width = 8; IsTabStop = true; Content = _hit; HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        AutomationProperties.SetName(this, name);
        _hit.PointerEntered += (_, _) => _hit.Background = Theme.Brush("#4478acdf");
        _hit.PointerExited += (_, _) => { if (!_dragging) _hit.Background = Theme.Brush("#00000000"); };
        _hit.PointerPressed += (_, e) =>
        {
            if (Parent is not UIElement parent || !e.GetCurrentPoint(_hit).Properties.IsLeftButtonPressed) return;
            _start = e.GetCurrentPoint(parent).Position.X; _dragging = true; Focus(FocusState.Pointer); _hit.CapturePointer(e.Pointer); Started?.Invoke(); e.Handled = true;
        };
        _hit.PointerMoved += (_, e) =>
        {
            if (!_dragging || Parent is not UIElement parent) return;
            Delta?.Invoke(e.GetCurrentPoint(parent).Position.X - _start); e.Handled = true;
        };
        _hit.PointerReleased += (_, e) =>
        {
            if (!_dragging) return; _dragging = false; _hit.ReleasePointerCapture(e.Pointer); Completed?.Invoke(); e.Handled = true;
        };
        _hit.PointerCanceled += (_, _) => Cancel(); _hit.PointerCaptureLost += (_, _) => Cancel();
        _hit.DoubleTapped += (_, e) => { Reset?.Invoke(); e.Handled = true; };
        KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape && _dragging) { Cancel(); e.Handled = true; }
            else if (e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right)
            { Started?.Invoke(); Delta?.Invoke(e.Key == Windows.System.VirtualKey.Left ? -10 : 10); Completed?.Invoke(); e.Handled = true; }
            else if (e.Key == Windows.System.VirtualKey.Home) { Reset?.Invoke(); e.Handled = true; }
        };
    }
    private void Cancel() { if (!_dragging) return; _dragging = false; _hit.ReleasePointerCaptures(); Canceled?.Invoke(); }
}
