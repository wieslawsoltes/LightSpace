using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public static class GeometryMapping
{
    public static SKMatrix PixelMatrix(GeometrySettings geometry, LensCorrectionSettings optics, int width, int height)
    {
        var transform = GeometryProjection.Create(geometry, optics, (double)width / height);
        var p = ProjectiveTransform.Scale(width, height) * transform * ProjectiveTransform.Scale(1d / width, 1d / height);
        return new((float)p.A, (float)p.B, (float)p.C, (float)p.D, (float)p.E, (float)p.F, (float)p.G, (float)p.H, (float)p.I);
    }
    public static SKPoint SourceToView(PhotoState state, int width, int height, SKRect destination, PointD source, bool uncropped = false)
    {
        var corrected = LensMapping.FromSource(source, state.Optics, (double)width / height);
        var frame = GeometryProjection.Create(state.Geometry, state.Optics, (double)width / height).Map(corrected);
        return PhotoTransform.SourceToView(uncropped ? new() : state.Crop, width, height, destination).MapPoint((float)frame.X * width, (float)frame.Y * height);
    }
    public static PointD ViewToSource(PhotoState state, int width, int height, SKRect destination, SKPoint view, bool uncropped = false)
    {
        var crop = PhotoTransform.SourceToView(uncropped ? new() : state.Crop, width, height, destination);
        var geometry = GeometryProjection.Create(state.Geometry, state.Optics, (double)width / height);
        if (!crop.TryInvert(out var inverse) || !geometry.TryInvert(out var projection)) return new(double.NaN, double.NaN);
        var point = inverse.MapPoint(view);
        return LensMapping.ToSource(projection.Map(new(point.X / width, point.Y / height)), state.Optics, (double)width / height);
    }
}
