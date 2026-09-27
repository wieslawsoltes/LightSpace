using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public sealed class ThumbnailCache : IDisposable
{
    private readonly PhotoRenderer _renderer=new();
    private readonly Dictionary<Guid,(long Revision,SKImage Image,long Used)> _cache=[];private long _clock;
    public SKImage Get(PhotoDocument photo)
    {
        if(_cache.TryGetValue(photo.Id,out var entry)&&entry.Revision==photo.Revision){_cache[photo.Id]=(entry.Revision,entry.Image,++_clock);return entry.Image;}
        if(entry.Image is not null){entry.Image.Dispose();_cache.Remove(photo.Id);}
        while(_cache.Count>=128){var first=_cache.MinBy(p=>p.Value.Used);first.Value.Image.Dispose();_cache.Remove(first.Key);}
        var (pw,ph)=photo.State.Crop.OutputSize(photo.Width,photo.Height);var scale=Math.Min(240f/pw,160f/ph);var w=Math.Max(1,(int)(pw*scale));var h=Math.Max(1,(int)(ph*scale));
        using var surface=SKSurface.Create(new SKImageInfo(w,h));surface.Canvas.Clear(SKColors.Transparent);_renderer.Draw(surface.Canvas,photo,SKRect.Create(w,h));var image=surface.Snapshot();_cache[photo.Id]=(photo.Revision,image,++_clock);return image;
    }
    public void Dispose(){foreach(var e in _cache.Values)e.Image.Dispose();_cache.Clear();_renderer.Dispose();}
}
public sealed class PhotoThumbnail(PhotoDocument photo,ThumbnailCache cache):SKCanvasElement
{
    protected override void RenderOverride(SKCanvas canvas,Size area)
    {
        var image=cache.Get(photo);var target=PhotoTransform.Fit(new(),image.Width,image.Height,SKRect.Create((float)area.Width,(float)area.Height));
        canvas.DrawImage(image,target,new SKSamplingOptions(SKFilterMode.Linear));
    }
}

public sealed class PhotoWrapPanel : Panel
{
    public double CellWidth{get;set;}=220;public double CellHeight{get;set;}=190;
    protected override Size MeasureOverride(Size availableSize)
    {
        var columns=Math.Max(1,(int)(double.IsFinite(availableSize.Width)?availableSize.Width/CellWidth:4));var width=double.IsFinite(availableSize.Width)?availableSize.Width:columns*CellWidth;
        foreach(var child in Children)child.Measure(new(width/columns,CellHeight));return new(width,Math.Ceiling(Children.Count/(double)columns)*CellHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns=Math.Max(1,(int)(finalSize.Width/CellWidth));var width=finalSize.Width/columns;
        for(var i=0;i<Children.Count;i++)Children[i].Arrange(new Rect(i%columns*width,i/columns*CellHeight,width,CellHeight));return finalSize;
    }
}
