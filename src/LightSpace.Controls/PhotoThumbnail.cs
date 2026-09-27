using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public sealed class ThumbnailCache : IDisposable
{
    private sealed record Entry(long Revision, byte[] Source, PhotoState State, SKImage Image, long Used);
    private readonly PhotoRenderer _renderer = new();
    private readonly Dictionary<Guid, Entry> _cache = [];
    private long _clock;

    public SKImage Get(PhotoDocument photo)
    {
        if (_cache.TryGetValue(photo.Id, out var entry))
        {
            if (entry.Revision == photo.Revision && ReferenceEquals(entry.Source, photo.Original)
                && ReferenceEquals(entry.State, photo.State))
            {
                _cache[photo.Id] = entry with { Used = ++_clock };
                return entry.Image;
            }
            entry.Image.Dispose(); _cache.Remove(photo.Id);
        }
        while (_cache.Count >= 128)
        {
            var oldest = _cache.MinBy(pair => pair.Value.Used);
            oldest.Value.Image.Dispose(); _cache.Remove(oldest.Key);
        }
        var (sourceWidth, sourceHeight) = photo.State.Crop.OutputSize(photo.Width, photo.Height);
        var scale = Math.Min(240f / sourceWidth, 160f / sourceHeight);
        var width = Math.Max(1, (int)(sourceWidth * scale));
        var height = Math.Max(1, (int)(sourceHeight * scale));
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(SKColors.Transparent);
        _renderer.Draw(surface.Canvas, photo, SKRect.Create(width, height));
        var image = surface.Snapshot();
        _cache[photo.Id] = new(photo.Revision, photo.Original, photo.State, image, ++_clock);
        return image;
    }

    public void Clear()
    {
        foreach (var entry in _cache.Values) entry.Image.Dispose();
        _cache.Clear(); _renderer.Clear();
    }
    public void Dispose() { Clear(); _renderer.Dispose(); }
}

public sealed class PhotoThumbnail(PhotoDocument photo, ThumbnailCache cache) : SKCanvasElement
{
    protected override void RenderOverride(SKCanvas canvas, Size area)
    {
        var image = cache.Get(photo);
        var target = PhotoTransform.Fit(new(), image.Width, image.Height,
            SKRect.Create((float)area.Width, (float)area.Height));
        canvas.DrawImage(image, target, new SKSamplingOptions(SKFilterMode.Linear));
    }
}

public sealed class PhotoWrapPanel : Panel
{
    public double CellWidth { get; set; } = 220;
    public double CellHeight { get; set; } = 190;
    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = Math.Max(1, (int)(double.IsFinite(availableSize.Width) ? availableSize.Width / CellWidth : 4));
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : columns * CellWidth;
        foreach (var child in Children) child.Measure(new(width / columns, CellHeight));
        return new(width, Math.Ceiling(Children.Count / (double)columns) * CellHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Math.Max(1, (int)(finalSize.Width / CellWidth));
        var width = finalSize.Width / columns;
        for (var i = 0; i < Children.Count; i++)
            Children[i].Arrange(new Rect(i % columns * width, i / columns * CellHeight, width, CellHeight));
        return finalSize;
    }
}
