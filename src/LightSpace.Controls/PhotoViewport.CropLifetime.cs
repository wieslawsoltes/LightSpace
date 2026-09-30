using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private XamlRoot? _cropRoot;
    private Size _cropRootSize, _cropSurfaceSize;
    private double _cropRasterizationScale;
    private Point _cropOrigin;

    private SKRect ImageFrame(PhotoDocument photo, Size area)
    {
        var available = SKRect.Create(26, 24, Math.Max(1, (float)area.Width - 52), Math.Max(1, (float)area.Height - 48));
        return PhotoTransform.Fit(DisplayCrop, photo.Width, photo.Height, available, Zoom, PanX, PanY);
    }

    // Observe only while captured. Root metrics can change before this control
    // receives SizeChanged; ancestor movement can change its origin without
    // changing its size at all. Both invalidate the pointer-down coordinate frame.
    private void StartCropTracking()
    {
        _cropRoot = XamlRoot;
        _cropRootSize = _cropRoot?.Size ?? default;
        _cropRasterizationScale = _cropRoot?.RasterizationScale ?? 1;
        _cropSurfaceSize = new(_surface.ActualWidth, _surface.ActualHeight);
        _cropOrigin = _surface.TransformToVisual(null).TransformPoint(new(0, 0));
        LayoutUpdated += CropLayoutUpdated;
        Unloaded += CropUnloaded;
        if (_cropRoot is not null) _cropRoot.Changed += CropRootChanged;
    }

    private void StopCropTracking()
    {
        LayoutUpdated -= CropLayoutUpdated;
        Unloaded -= CropUnloaded;
        if (_cropRoot is not null) _cropRoot.Changed -= CropRootChanged;
        _cropRoot = null;
    }

    private void CropLayoutUpdated(object? sender, object args)
    {
        if (HasCropGesture && !CropFrameIsCurrent()) Cancel();
    }

    private void CropRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (HasCropGesture && !CropFrameIsCurrent()) Cancel();
    }

    private void CropUnloaded(object sender, RoutedEventArgs args) => Cancel();

    private bool CropFrameIsCurrent()
    {
        static bool Same(double a, double b) => Math.Abs(a - b) < .01;
        if (_session.Active is not { } photo || photo.Id != _cropPhoto || !ReferenceEquals(_cropRoot, XamlRoot))
            return false;
        if (_cropRoot is { } root && (!root.IsHostVisible
            || !Same(root.Size.Width, _cropRootSize.Width) || !Same(root.Size.Height, _cropRootSize.Height)
            || Math.Abs(root.RasterizationScale - _cropRasterizationScale) > .000001))
            return false;
        var size = new Size(_surface.ActualWidth, _surface.ActualHeight);
        if (!Same(size.Width, _cropSurfaceSize.Width) || !Same(size.Height, _cropSurfaceSize.Height))
            return false;
        try
        {
            var origin = _surface.TransformToVisual(null).TransformPoint(new(0, 0));
            return Same(origin.X, _cropOrigin.X) && Same(origin.Y, _cropOrigin.Y)
                && ImageFrame(photo, size) == _cropInteractionFrame;
        }
        catch (InvalidOperationException) { return false; } // Disconnected/reparented visual.
    }
}
