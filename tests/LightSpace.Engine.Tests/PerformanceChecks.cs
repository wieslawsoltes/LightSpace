using System.Diagnostics;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class PerformanceChecks
{
    private sealed record Measurement(int Iterations, double Milliseconds, long AllocatedBytes, int EqualResults);
    private static Measurement Measure(Func<bool> compare, int iterations)
    {
        for (var i = 0; i < 100; i++) compare();
        var clock = Stopwatch.StartNew(); var start = GC.GetAllocatedBytesForCurrentThread(); var equal = 0;
        for (var i = 0; i < iterations; i++) if (compare()) equal++;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - start; clock.Stop();
        return new(iterations, clock.Elapsed.TotalMilliseconds, allocated, equal);
    }
    public static void Run()
    {
        var a = new PhotoState { Develop = new() { Exposure = .5f }, Masks = [new()] };
        var b = a with { Develop = a.Develop with { Mixer = a.Develop.Mixer.ToArray() }, Masks = a.Masks.ToArray() };
        var baseline = Measure(() => CatalogSerializer.SerializeSettings(a) == CatalogSerializer.SerializeSettings(b), 10000);
        var optimized = Measure(() => PhotoStateEquality.All(a, b), 10000);
        using var renderer = new PhotoRenderer(); var photo = Tiny(); photo.State = a;
        using var surface = SKSurface.Create(new SKImageInfo(96, 64)); renderer.Draw(surface.Canvas, photo, SKRect.Create(96, 64)); var before = renderer.Statistics;
        var clock = Stopwatch.StartNew();
        for (var i = 0; i < 250; i++) { photo.State = photo.State with { Rating = i % 6, Caption = "Metadata " + i }; photo.Revision++; renderer.Draw(surface.Canvas, photo, SKRect.Create(96, 64)); }
        clock.Stop(); var after = renderer.Statistics;
        Directory.CreateDirectory("artifacts/engine");
        File.WriteAllText("artifacts/engine/performance.json", JsonSerializer.Serialize(new
        {
            workload = "10,000 equivalent-state comparisons; 250 warm 96x64 raster metadata redraws",
            timingScope = "CPU microbenchmark on this CI host; not end-to-end UI or physical-GPU performance",
            baselinePolicy = "Previous serialization-based state comparison",
            serializationComparison = baseline, valueComparison = optimized,
            metadataRedraws = new { count = 250, milliseconds = clock.Elapsed.TotalMilliseconds, additionalDecodes = after.ImageDecodes - before.ImageDecodes, additionalShaders = after.ShaderBuilds - before.ShaderBuilds, cacheBytes = after.CachedBytes }
        }, new JsonSerializerOptions { WriteIndented = true }));
        Check(baseline.EqualResults == 10000 && optimized.EqualResults == 10000);
        Check(optimized.AllocatedBytes < baseline.AllocatedBytes / 10, "Comparison allocation regression.");
        Check(after.ImageDecodes == before.ImageDecodes && after.ShaderBuilds == before.ShaderBuilds, "Metadata triggered pixel processing rebuilds.");
    }
}
