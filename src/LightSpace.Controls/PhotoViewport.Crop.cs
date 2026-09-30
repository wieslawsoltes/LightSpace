using LightSpace.Core;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private CropGesture? _cropGesture;
    private CropHandle _cropHandle;
    private SKRect _cropInteractionFrame;
    private Point _cropPress;
    private Guid _cropPhoto;
    private uint _cropPointer;
    private bool _cropAspectLocked;
    private CropGuide _cropGuide;
    private bool _cropGuideReversed;
    public event Action? CropToolChanged;
    public bool HasCropGesture => _cropGesture is not null;
    public bool CropAspectLocked
    {
        get => _cropAspectLocked;
        set { if (_cropAspectLocked == value) return; Cancel(); _cropAspectLocked = value; CropToolChanged?.Invoke(); }
    }
    public CropGuide CropGuide => _cropGuide;
    public bool CropGuideReversed => _cropGuideReversed;

    public void SetCropGuide(CropGuide guide, bool reversed = false)
    {
        if (!Enum.IsDefined(guide)) throw new ArgumentOutOfRangeException(nameof(guide));
        if (_cropGuide == guide && _cropGuideReversed == reversed) return;
        _cropGuide = guide; _cropGuideReversed = reversed; Invalidate(); CropToolChanged?.Invoke();
    }
    public void CycleCropGuide() => SetCropGuide((CropGuide)(((int)_cropGuide + 1) % 6), _cropGuideReversed);
    public void ReverseCropGuide() => SetCropGuide(_cropGuide, !_cropGuideReversed);

    public void ApplyCropAspect(double outputAspect)
    {
        if (_session.Active is not { } photo) return;
        // Validate before canceling an existing edit, then calculate from the committed state.
        _ = CropGeometry.FrameAspect(outputAspect, photo.State.Crop, photo.Width, photo.Height);
        Cancel();
        var crop = CropGeometry.WithAspect(photo.State.Crop, outputAspect, photo.Width, photo.Height);
        _cropAspectLocked = true;
        _session.Edit("Crop aspect ratio", state => state with { Crop = crop });
        CropToolChanged?.Invoke();
    }
    public void RestoreOriginalCropAspect()
    {
        Cancel(); _cropAspectLocked = true;
        _session.Edit("Original crop framing", state => state with
        { Crop = state.Crop with { Left = 0, Top = 0, Right = 1, Bottom = 1 } });
        CropToolChanged?.Invoke();
    }
    public void SwapCropOrientation()
    {
        if (_session.Active is not { } photo) return;
        Cancel();
        var crop = CropGeometry.WithAspect(photo.State.Crop, 1 / CropGeometry.OutputAspect(photo.State.Crop, photo.Width, photo.Height), photo.Width, photo.Height);
        _session.Edit("Swap crop orientation", state => state with { Crop = crop });
        CropToolChanged?.Invoke();
    }
    public void NudgeCrop(int dx, int dy)
    {
        if (_session.Active is not { } photo || _dragging) return;
        var gesture = new CropGesture(photo.State.Crop, CropHandle.Move, new(0, 0), photo.Width, photo.Height);
        var bounds = gesture.Evaluate(new((double)dx / photo.Width, (double)dy / photo.Height));
        _session.Edit("Move crop", state => state with { Crop = CropGeometry.Apply(state.Crop, bounds) });
    }

    private static readonly CropHandle[] CropHandles = [CropHandle.TopLeft, CropHandle.Top, CropHandle.TopRight, CropHandle.Right,
        CropHandle.BottomRight, CropHandle.Bottom, CropHandle.BottomLeft, CropHandle.Left];
    private static readonly string[] CropHandleIds = ["crop-top-left", "crop-top", "crop-top-right", "crop-right",
        "crop-bottom-right", "crop-bottom", "crop-bottom-left", "crop-left"];
    private SKRect CropViewRect(CropSettings crop)
    {
        var a = FrameToView(crop.Left, crop.Top); var b = FrameToView(crop.Right, crop.Bottom);
        return new(a.X, a.Y, b.X, b.Y);
    }
    private static SKPoint CropPoint(SKRect rect, int index) => index switch
    {
        0 => new(rect.Left, rect.Top), 1 => new(rect.MidX, rect.Top), 2 => new(rect.Right, rect.Top), 3 => new(rect.Right, rect.MidY),
        4 => new(rect.Right, rect.Bottom), 5 => new(rect.MidX, rect.Bottom), 6 => new(rect.Left, rect.Bottom), _ => new(rect.Left, rect.MidY)
    };
    private void AddCropHandles(List<ViewportHandle> handles, CropSettings crop)
    {
        var rect = CropViewRect(crop);
        for (var i = 0; i < 8; i++) { var p = CropPoint(rect, i); handles.Add(new(CropHandleIds[i], p.X, p.Y)); }
        handles.Add(new("crop-center", rect.MidX, rect.MidY));
    }
    private void PressCrop(PointerRoutedEventArgs e)
    {
        if (_dragging || _session.Active is not { } photo || _imageRect.Width <= 0 || _imageRect.Height <= 0) return;
        var pointer = e.GetCurrentPoint(_surface); var point = pointer.Position;
        if (!pointer.Properties.IsLeftButtonPressed) return;
        Focus(FocusState.Pointer); e.Handled = true;
        var rect = CropViewRect(photo.State.Crop); var selected = -1; var nearest = 12d * 12;
        for (var i = 0; i < 8; i++)
        {
            var handle = CropPoint(rect, i); var x = point.X - handle.X; var y = point.Y - handle.Y; var distance = x * x + y * y;
            if (distance <= nearest) { selected = i; nearest = distance; }
        }
        if (selected < 0 && !_imageRect.Contains((float)point.X, (float)point.Y)) return;
        _cropHandle = selected >= 0 ? CropHandles[selected]
            : rect.Contains((float)point.X, (float)point.Y) && (photo.State.Crop.Width < .999f || photo.State.Crop.Height < .999f)
                ? CropHandle.Move : CropHandle.Create;
        static bool Down(VirtualKey key) => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var locked = _cropAspectLocked || e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) || Down(VirtualKey.Shift);
        var centered = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu) || Down(VirtualKey.Menu);
        var frame = new PointD((point.X - _imageRect.Left) / _imageRect.Width, (point.Y - _imageRect.Top) / _imageRect.Height);
        if (!_surface.CapturePointer(e.Pointer)) return;
        _cropPointer = e.Pointer.PointerId; _cropPhoto = photo.Id; _cropPress = point; _cropInteractionFrame = _imageRect;
        _cropGesture = new(photo.State.Crop, _cropHandle, frame, photo.Width, photo.Height, locked, centered);
        _startState = photo.State; _dragging = true; Focus(FocusState.Pointer); _session.BeginGesture(); e.Handled = true;
    }
    private void MoveCrop(PointerRoutedEventArgs e)
    {
        if (!_dragging || _cropGesture is not { } gesture || e.Pointer.PointerId != _cropPointer) return;
        if (_session.Active is not { } photo || photo.Id != _cropPhoto) { Cancel(); return; }
        var p = e.GetCurrentPoint(_surface).Position;
        if (_cropHandle == CropHandle.Create && Math.Abs(p.X - _cropPress.X) + Math.Abs(p.Y - _cropPress.Y) < 5) return;
        var frame = new PointD((p.X - _cropInteractionFrame.Left) / _cropInteractionFrame.Width, (p.Y - _cropInteractionFrame.Top) / _cropInteractionFrame.Height);
        var crop = CropGeometry.Apply(photo.State.Crop, gesture.Evaluate(frame));
        if (!ReferenceEquals(crop, photo.State.Crop)) _session.Preview(state => state with { Crop = crop });
        e.Handled = true;
    }
    private void PaintCrop(SKCanvas canvas, CropSettings crop)
    {
        var rect = CropViewRect(crop);
        using var paint = new SKPaint { Color = new SKColor(0, 0, 0, 155), IsAntialias = true };
        canvas.Save(); canvas.ClipRect(_imageRect); canvas.ClipRect(rect, SKClipOperation.Difference); canvas.DrawRect(_imageRect, paint); canvas.Restore();
        paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1; paint.Color = SKColors.White.WithAlpha(150);
        Span<CropGuideLine> lines = stackalloc CropGuideLine[CropGuides.MaximumLines];
        var count = CropGuides.Write(_cropGuide, new(rect.Left, rect.Top, rect.Width, rect.Height), _cropGuideReversed, lines);
        canvas.Save(); canvas.ClipRect(_imageRect); canvas.ClipRect(rect);
        for (var i = 0; i < count; i++) canvas.DrawLine((float)lines[i].Start.X, (float)lines[i].Start.Y, (float)lines[i].End.X, (float)lines[i].End.Y, paint);
        canvas.Restore(); paint.Color = SKColors.White; canvas.DrawRect(rect, paint);
        paint.Style = SKPaintStyle.Fill;
        for (var i = 0; i < 8; i++) { var p = CropPoint(rect, i); canvas.DrawRect(p.X - 3, p.Y - 3, 6, 6, paint); }
    }
}
