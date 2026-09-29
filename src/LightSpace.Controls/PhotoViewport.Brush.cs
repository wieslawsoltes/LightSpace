using LightSpace.Core;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private BrushStrokeBuilder? _strokeBuilder;
    private BrushStroke? _brushStart;
    private LocalMask? _brushMask;
    private SKPoint? _brushCursor;
    private int _brushDabLimit;
    private bool _usePressure;
    public BrushStroke BrushSettings { get; set; } = new();
    private void PressBrush(PointerRoutedEventArgs e)
    {
        if (_session.Active is not { } photo) return;
        var point = e.GetCurrentPoint(_surface); var screen = point.Position;
        if (!_imageRect.Contains((float)screen.X, (float)screen.Y) || !ValidSource(ToSource(screen))) return;
        var selected = ActiveMask >= 0 && ActiveMask < photo.State.Masks.Length ? photo.State.Masks[ActiveMask] : null;
        if (selected is null && photo.State.Masks.Length >= 8) { Status?.Invoke("Select an existing mask or remove one before painting another."); return; }
        selected ??= new LocalMask { Kind = MaskKind.Brush, Name = "Brush " + (photo.State.Masks.Length + 1) };
        if (selected.Strokes.Length >= BrushStroke.MaximumStrokes) { Status?.Invoke("The selected mask has reached its 64-stroke limit."); return; }
        _brushDabLimit = Math.Min(BrushStroke.MaximumDabs, 65536 - selected.Strokes.Sum(s => s.Dabs.Length));
        if (_brushDabLimit <= 0) { Status?.Invoke("The selected mask has reached its dab limit."); return; }
        var alt = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu)
            || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        Focus(FocusState.Pointer); _startState = photo.State; _brushMask = selected;
        if (ActiveMask < 0 || ActiveMask >= photo.State.Masks.Length) ActiveMask = photo.State.Masks.Length;
        _brushStart = BrushSettings.Normalize() with { Id = Guid.NewGuid(), Dabs = [], Erase = BrushSettings.Erase || alt };
        _strokeBuilder = new(_brushStart, (float)photo.Width / photo.Height);
        _usePressure = e.Pointer.PointerDeviceType.ToString() == "Pen";
        _dragging = true; _surface.CapturePointer(e.Pointer); _session.BeginGesture(); MoveBrush(e); e.Handled = true;
    }
    private void MoveBrush(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_surface); var uv = ToSource(point.Position);
        if (!ValidSource(uv)) { _brushCursor = null; Invalidate(); return; }
        _brushCursor = uv;
        if (!_dragging || _strokeBuilder is null || _brushMask is null || _startState is null) { Invalidate(); return; }
        var oldCount = _strokeBuilder.Count;
        if (oldCount >= _brushDabLimit) { Invalidate(); return; }
        _strokeBuilder.Add(uv.X, uv.Y, _usePressure ? point.Properties.Pressure : 1);
        if (_strokeBuilder.Count == oldCount) return;
        var stroke = _strokeBuilder.Snapshot();
        if (stroke.Dabs.Length > _brushDabLimit) stroke = stroke with { Dabs = stroke.Dabs[.._brushDabLimit] };
        var changed = _brushMask with { Strokes = [.. _brushMask.Strokes, stroke] };
        var masks = _startState.Masks.Any(m => m.Id == changed.Id)
            ? _startState.Masks.Select(m => m.Id == changed.Id ? changed : m).ToArray() : [.. _startState.Masks, changed];
        _session.Preview(s => s with { Masks = masks });
        if (_strokeBuilder.Count >= _brushDabLimit) Status?.Invoke("Stroke limit reached. Release the pointer to finish this stroke.");
        e.Handled = true;
    }
    private void PaintBrushCursor(SKCanvas canvas, PhotoDocument photo)
    {
        if (_brushCursor is not { } uv) return;
        var settings = BrushSettings.Normalize(); var aspect = (float)photo.Width / photo.Height;
        var center = ToView(uv.X, uv.Y);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1, Color = SKColors.White };
        canvas.Save(); canvas.ClipRect(_imageRect);
        foreach (var radius in new[] { settings.Radius, settings.Radius * (1 - settings.Feather) })
        {
            using var path = new SKPath();
            for (var i = 0; i <= 48; i++)
            {
                var angle = i * MathF.PI / 24;
                var p = ToView(uv.X + MathF.Cos(angle) * radius / aspect, uv.Y + MathF.Sin(angle) * radius);
                if (i == 0) path.MoveTo(p); else path.LineTo(p);
            }
            path.Close(); canvas.DrawPath(path, paint); paint.Color = SKColors.White.WithAlpha(130);
        }
        canvas.DrawLine(center.X - 4, center.Y, center.X + 4, center.Y, paint);
        if (!(_brushStart?.Erase ?? settings.Erase)) canvas.DrawLine(center.X, center.Y - 4, center.X, center.Y + 4, paint);
        canvas.Restore();
    }
}
