namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    public void PublishDiagnostics()
    {
        if (DiagnosticsChanged is null || !IsLoaded || _disposed) return;
        var list = new List<WidgetBounds>();
        foreach (var (id, reference) in _widgets)
        {
            if (!reference.TryGetTarget(out var widget) || !widget.IsLoaded || widget.ActualWidth < 1 || widget.ActualHeight < 1) continue;
            var visible = true; DependencyObject? parent = widget;
            while (parent is not null) { if (parent is FrameworkElement element && element.Visibility == Visibility.Collapsed) { visible = false; break; } parent = VisualTreeHelper.GetParent(parent); }
            if (!visible) continue;
            try
            {
                var rect = widget.TransformToVisual(this).TransformBounds(new Rect(0, 0, widget.ActualWidth, widget.ActualHeight));
                if (rect.Right > 0 && rect.Bottom > 0 && rect.X < ActualWidth && rect.Y < ActualHeight) list.Add(new(id, rect.X, rect.Y, rect.Width, rect.Height));
            }
            catch (InvalidOperationException) { }
        }
        if (!_gridMode && !_surveyMode)
        {
            var origin = Viewport.TransformToVisual(this).TransformPoint(new(0, 0));
            var image = Viewport.ImageBounds; list.Add(new("image", origin.X + image.X, origin.Y + image.Y, image.Width, image.Height));
            foreach (var handle in Viewport.InteractionHandles()) list.Add(new(handle.Id, origin.X + handle.X - 7, origin.Y + handle.Y - 7, 14, 14));
            if (_referenceOpen && _referenceView is not null)
            {
                var position = _referenceView.TransformToVisual(this).TransformPoint(new(0, 0)); var bounds = _referenceView.ImageBounds;
                list.Add(new("reference-image", position.X + bounds.X, position.Y + bounds.Y, bounds.Width, bounds.Height));
            }
        }
        var p = Session.Active;
        var masks = p?.State.Masks ?? [];
        var summaries = masks.Select(m => m with { Strokes = [] }).ToArray();
        var brush = masks.Select(m => new BrushDiagnostic(m.Id, m.Strokes.Length, m.Strokes.Sum(s => s.Dabs.Length), m.Strokes.LastOrDefault()?.Erase ?? false)).ToArray();
        DiagnosticsChanged.Invoke(new(p?.Name ?? "", _surveyMode ? "Survey" : _gridMode ? "Grid" : "Detail", Viewport.Tool.ToString(), p?.State.Develop.Exposure ?? 0, p?.State.Rating ?? 0,
            Session.Catalog.Photos.Count, masks.Length, p?.State.CloneSpots.Length ?? 0, Session.CanUndo, Session.CanRedo, Session.Revision, _status.Text,
            list.ToArray(), _recovery.Status, p?.State.Develop.Grading ?? new(), summaries, Viewport.ComparisonPosition,
            new(_renderer.Statistics, _thumbnails.Statistics, _thumbnails.Renders, _cardBuilds, _libraryBuilds, _inspectorBuilds),
            p?.State.Develop.Channels ?? new(), brush, _renderer.BrushStatistics, _renderer.CurveLookupBuilds, p?.State.Caption ?? "", p?.State.Keywords ?? [],
            _persistence?.Statistics, _renderer.SourceSamplePixels, Photography(p), ReferenceInfo()) { Survey = SurveyInfo() });
    }
    private void AdvancedKeyboard(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || _dialogOverlay is not null || (XamlRoot is { } root && FocusManager.GetFocusedElement(root) is TextBox)) return;
        bool Down(VirtualKey key) => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (Down(VirtualKey.Control) || Down(VirtualKey.LeftWindows) || Down(VirtualKey.RightWindows)) return;
        if (e.Key == VirtualKey.B) { ChooseBrush(false); e.Handled = true; }
        else if (Viewport.Tool == PhotoTool.Brush && (int)e.Key is 219 or 221)
        {
            var factor = (int)e.Key == 219 ? .8f : 1.25f;
            Viewport.BrushSettings = (Viewport.BrushSettings with { Radius = Viewport.BrushSettings.Radius * factor }).Normalize();
            Viewport.Invalidate(); e.Handled = true;
        }
    }
}
