using System.Runtime.CompilerServices;
using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

/// <summary>Shared source/display mapping. Weakly keyed projection caches avoid repeating constrained-crop solves for each mask-outline point.</summary>
public static class GeometryMapping
{
    private sealed record Projection(ProjectiveTransform Forward, ProjectiveTransform Inverse);
    private sealed class OpticalCache
    {
        public readonly Dictionary<double, Projection> Aspects = [];
    }
    private sealed class GeometryCache
    {
        public readonly ConditionalWeakTable<LensCorrectionSettings, OpticalCache> Optics = new();
    }
    private static readonly ConditionalWeakTable<GeometrySettings, GeometryCache> Cache = new();
    private static Projection GetProjection(GeometrySettings geometry, LensCorrectionSettings optics, double aspect)
    {
        var cache = Cache.GetValue(geometry, static _ => new()).Optics.GetValue(optics, static _ => new());
        lock (cache.Aspects)
        {
            if (cache.Aspects.TryGetValue(aspect, out var result)) return result;
            var forward = GeometryProjection.Create(geometry, optics, aspect);
            if (!forward.TryInvert(out var inverse)) throw new InvalidOperationException("Geometry projection is singular.");
            if (cache.Aspects.Count >= 16) cache.Aspects.Clear();
            result = new(forward, inverse); cache.Aspects.Add(aspect, result); return result;
        }
    }
    public static SKMatrix PixelMatrix(GeometrySettings geometry, LensCorrectionSettings optics, int width, int height)
    {
        var transform = GetProjection(geometry, optics, (double)width / height).Forward;
        var p = ProjectiveTransform.Scale(width, height) * transform * ProjectiveTransform.Scale(1d / width, 1d / height);
        return new((float)p.A, (float)p.B, (float)p.C, (float)p.D, (float)p.E, (float)p.F, (float)p.G, (float)p.H, (float)p.I);
    }
    public static SKPoint SourceToView(PhotoState state, int width, int height, SKRect destination, PointD source, bool uncropped = false)
    {
        var aspect = (double)width / height;
        var corrected = LensMapping.FromSource(source, state.Optics, aspect);
        var frame = GetProjection(state.Geometry, state.Optics, aspect).Forward.Map(corrected);
        return PhotoTransform.SourceToView(uncropped ? new() : state.Crop, width, height, destination).MapPoint((float)frame.X * width, (float)frame.Y * height);
    }
    public static PointD ViewToSource(PhotoState state, int width, int height, SKRect destination, SKPoint view, bool uncropped = false)
    {
        var crop = PhotoTransform.SourceToView(uncropped ? new() : state.Crop, width, height, destination);
        if (!crop.TryInvert(out var inverse)) return new(double.NaN, double.NaN);
        var point = inverse.MapPoint(view); var aspect = (double)width / height;
        return LensMapping.ToSource(GetProjection(state.Geometry, state.Optics, aspect).Inverse.Map(new(point.X / width, point.Y / height)), state.Optics, aspect);
    }
}
