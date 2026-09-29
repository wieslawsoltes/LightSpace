using System.Text.Json;
using System.Text.Json.Nodes;
using LightSpace.Catalog;
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class PhotographyTests
{
    private static void Near(double a, double b, double tolerance = .00001) => Check(Math.Abs(a - b) <= tolerance, $"Expected {a} ~= {b}");
    public static void Register(Action<string, Action> test)
    {
        test("Geometry and optical settings normalize finite bounded values", () =>
        {
            var g = new GeometrySettings { Rotate = float.NaN, Scale = float.PositiveInfinity, Vertical = 900 }.Normalize();
            Check(g is { Rotate: 0, Scale: 100, Vertical: 100 } && ReferenceEquals(g, g.Normalize()));
            var o = new LensCorrectionSettings(float.NaN, 200, -200).Normalize(); Check(o is { Distortion: 0, Vignetting: 100, RedCyan: -100 });
        });
        test("Identity geometry preserves coordinates at different source aspects", () =>
        {
            foreach (var aspect in new[] { .3, 1, 1.5, 4, 25 })
            {
                var p = GeometryProjection.Create(new(), new(), aspect).Map(new(.23, .71)); Near(p.X, .23); Near(p.Y, .71);
            }
        });
        test("Projective geometry inversion roundtrips bounded settings", () =>
        {
            var random = new Random(1827);
            for (var i = 0; i < 200; i++)
            {
                float Next() => (float)(random.NextDouble() * 200 - 100);
                var g = new GeometrySettings { Rotate = Next() * .4f, Horizontal = Next(), Vertical = Next(), Aspect = Next(), Scale = 100 + Next() * .45f, XOffset = Next(), YOffset = Next() };
                var h = GeometryProjection.Create(g, new(), 1.5); Check(h.TryInvert(out var inverse));
                var p = new PointD(random.NextDouble(), random.NextDouble()); var restored = inverse.Map(h.Map(p)); Near(restored.X, p.X); Near(restored.Y, p.Y);
            }
        });
        test("Optical forward and inverse mappings agree without iteration", () =>
        {
            foreach (var d in new[] { -100, -45, 0, 45, 100 }) foreach (var aspect in new[] { .5, 1, 2d })
            {
                var o = new LensCorrectionSettings(d);
                for (var x = 1; x < 10; x++) for (var y = 1; y < 10; y++)
                {
                    var p = new PointD(x / 10d, y / 10d); var raw = LensMapping.ToSource(p, o, aspect); var q = LensMapping.FromSource(raw, o, aspect);
                    Near(q.X, p.X); Near(q.Y, p.Y);
                }
            }
        });
        test("Constrained geometry covers frame corners including thin panoramas", () =>
        {
            foreach (var aspect in new[] { .02, .5, 1.5, 50d })
            {
                var o = new LensCorrectionSettings(80, 0, 100, -100);
                var g = new GeometrySettings { Rotate = 35, Vertical = -80, Horizontal = 65, XOffset = 30, YOffset = -30, Scale = 50, ConstrainCrop = true };
                var h = GeometryProjection.Create(g, o, aspect); Check(h.TryInvert(out var inverse));
                foreach (var point in new[] { new PointD(0, 0), new(1, 0), new(1, 1), new(0, 1) })
                {
                    var source = LensMapping.ToSource(inverse.Map(point), o, aspect);
                    Check(source.X is >= 0 and <= 1 && source.Y is >= 0 and <= 1, $"Source corner out of range: {source}");
                }
            }
        });
        test("Source hit testing inverts crop flips rotations optics and geometry", () =>
        {
            for (var quarter = 0; quarter < 4; quarter++) foreach (var flip in new[] { false, true })
            {
                var s = new PhotoState { Crop = new(.1f, .15f, .9f, .85f, quarter, flip, !flip), Geometry = new() { Rotate = 17, Vertical = 25, Horizontal = -20 }, Optics = new(50) };
                var target = SKRect.Create(20, 30, 500, 420); var p = new PointD(.4, .55);
                var displayed = GeometryMapping.SourceToView(s, 600, 400, target, p); var restored = GeometryMapping.ViewToSource(s, 600, 400, target, displayed);
                Near(p.X, restored.X); Near(p.Y, restored.Y);
            }
        });
        test("Straightening respects source aspect and horizon direction", () =>
        {
            var angle = GeometryProjection.HorizonCorrection(new(.2, .4), new(.8, .5), 1.5);
            Near(angle, -Math.Atan2(.1, .9) * 180 / Math.PI);
            Near(angle, GeometryProjection.HorizonCorrection(new(.8, .5), new(.2, .4), 1.5));
        });
        test("Projective export retains alpha outside unconstrained rotated source", () =>
        {
            var p = Solid(SKColors.Gray, 300, 200); p.State = new() { Geometry = new() { Rotate = 25 } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            Check(b.GetPixel(2, 2).Alpha == 0 && b.GetPixel(150, 100).Alpha == 255);
        });
        test("Constrained optical and geometric export fills the output", () =>
        {
            var p = Solid(SKColors.Gray, 300, 200);
            p.State = new() { Geometry = new() { Rotate = 25, Vertical = -60, ConstrainCrop = true }, Optics = new(70, 0, 70, -70) };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            foreach (var (x, y) in new[] { (2, 2), (297, 2), (297, 197), (2, 197), (150, 100) }) Check(b.GetPixel(x, y).Alpha >= 250);
        });
        test("Optical shader sampling agrees with the source-coordinate model", () =>
        {
            var p = Tiny(); p.State = new() { Optics = new(70) };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            for (var y = 10; y < 55; y += 10) for (var x = 15; x < 80; x += 10)
            {
                var q = LensMapping.ToSource(new((x + .5) / 96, (y + .5) / 64), p.State.Optics, 1.5); var c = b.GetPixel(x, y);
                Near(c.Red, 35 + (q.X * 96 - .5) * 2, 3); Near(c.Green, 35 + (q.Y * 64 - .5) * 3, 3);
            }
        });
        test("Lens falloff correction changes edges rather than the center", () =>
        {
            var p = Solid(new SKColor(96, 96, 96), 300, 200); p.State = new() { Optics = new(0, 60) };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            Near(b.GetPixel(150, 100).Red, 96, 2); Check(b.GetPixel(5, 5).Red > 130);
        });
        test("Chromatic alignment changes channel sampling while preserving green", () =>
        {
            var p = GridFixture(); p.State = new() { Optics = new(0, 0, 100, -100) };
            using var r = new PhotoRenderer(); using var before = SKBitmap.Decode(p.Original); using var after = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            var changed = false;
            for (var y = 30; y < 450; y += 7) for (var x = 30; x < 610; x += 7)
            { var a = before.GetPixel(x, y); var b = after.GetPixel(x, y); Near(a.Green, b.Green, 2); changed |= Math.Abs(a.Red - b.Red) > 10 || Math.Abs(a.Blue - b.Blue) > 10; }
            Check(changed);
        });
        test("Clipping overlays are view-only and never contaminate histogram or export", () =>
        {
            using var r = new PhotoRenderer(); using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            foreach (var color in new[] { SKColors.Black, SKColors.White })
            {
                var p = Solid(color); var png = r.Export(p, SKEncodedImageFormat.Png); var histogram = r.CalculateHistogram(p);
                r.Draw(surface.Canvas, p, SKRect.Create(96, 64), clipping: ClippingIndicators.Both);
                using var image = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(image); var shown = bitmap.GetPixel(40, 30);
                Check(color == SKColors.Black ? shown.Blue > 240 && shown.Red < 40 : shown.Red > 240 && shown.Blue < 40);
                Check(png.SequenceEqual(r.Export(p, SKEncodedImageFormat.Png)));
                Check(histogram.Red.SequenceEqual(r.CalculateHistogram(p).Red));
            }
        });
        test("Relative white balance neutralizes a compatible source sample", () =>
        {
            var p = Solid(new SKColor(132, 128, 124)); var e = WhiteBalanceEstimator.Estimate(new(132 / 255f, 128 / 255f, 124 / 255f))!;
            Check(!e.WasClamped); p.State = new() { Develop = new() { Temperature = e.Temperature, Tint = e.Tint } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var pixel = b.GetPixel(40, 30);
            Near(pixel.Red, pixel.Green, 2); Near(pixel.Blue, pixel.Green, 2);
        });
        test("White balance rejects black and reports saturated correction limits", () =>
        {
            Check(WhiteBalanceEstimator.Estimate(new(0, 0, 0)) is null);
            Check(WhiteBalanceEstimator.Estimate(new(.9f, .2f, .1f)) is { WasClamped: true });
        });
        test("New geometry and optics settings synchronize without replacing metadata", () =>
        {
            var catalog = Catalog(); var s = new EditorSession(catalog);
            s.Edit("Corrections", x => x with { Geometry = new() { Rotate = 8, ConstrainCrop = true }, Optics = new(30) });
            catalog.Photos[1].State = catalog.Photos[1].State with { Rating = 4 }; s.Selection.UnionWith(catalog.Photos.Select(p => p.Id));
            s.SyncSelected(); Check(catalog.Photos[1].State.Geometry.Rotate == 8 && catalog.Photos[1].State.Optics.Distortion == 30 && catalog.Photos[1].State.Rating == 4);
            s.Undo(); Check(catalog.Photos[1].State.Geometry.IsIdentity && catalog.Photos[1].State.Rating == 4);
        });
        test("Geometry and optical corrections roundtrip through native XMP and catalogs", () =>
        {
            var state = new PhotoState { Geometry = new() { Rotate = 17, Horizontal = 18, Scale = 123, ConstrainCrop = true }, Optics = new(44, -20, 15, -8) };
            var p = Tiny(); p.State = state; var catalog = new CatalogDocument { Photos = [p], ActivePhoto = p.Id };
            Check(PhotoStateEquality.All(state, CatalogSerializer.Deserialize(CatalogSerializer.Serialize(catalog)).Photos[0].State));
            var xmp = XmpSidecar.Export(state); Check(xmp.Warnings.Any(w => w.Contains("optics"))); Check(PhotoStateEquality.All(state, XmpSidecar.Import(xmp.Xml).State));
        });
        test("Schemas one through four migrate missing geometry to neutral defaults", () =>
        {
            for (var version = 1; version < CatalogDocument.CurrentSchemaVersion; version++)
            {
                var node = JsonNode.Parse(CatalogSerializer.Serialize(Catalog()))!; node["SchemaVersion"] = version;
                foreach (var photo in node["Photos"]!.AsArray()) { photo!["State"]!.AsObject().Remove("Geometry"); photo["State"]!.AsObject().Remove("Optics"); }
                var loaded = CatalogSerializer.Deserialize(node.ToJsonString());
                Check(loaded.SchemaVersion == CatalogDocument.CurrentSchemaVersion && loaded.Photos.All(p => p.State.Geometry.IsIdentity && p.State.Optics.IsNeutral));
            }
        });
        test("Geometry previews remain outside committed recovery and cancel cleanly", () =>
        {
            var s = new EditorSession(Catalog()); s.Preview(x => x with { Geometry = new() { Rotate = 23 } });
            var persisted = CatalogSerializer.Deserialize(s.CaptureCommittedSnapshot().Json); Check(persisted.Photos[0].State.Geometry.IsIdentity);
            s.CancelGesture(); Check(s.Active!.State.Geometry.IsIdentity && !s.CanUndo);
        });
        test("Composed shaders survive deferred drawing after cache disposal", () =>
        {
            var p = Tiny(); p.State = new() { Develop = new() { Exposure = .3f }, Optics = new(40, 25, 20, -20), Geometry = new() { Rotate = 15, ConstrainCrop = true } };
            using var recorder = new SKPictureRecorder(); var canvas = recorder.BeginRecording(SKRect.Create(200, 150));
            using (var renderer = new PhotoRenderer()) renderer.Draw(canvas, p, SKRect.Create(200, 150));
            using var picture = recorder.EndRecording(); GC.Collect(); GC.WaitForPendingFinalizers();
            using var surface = SKSurface.Create(new SKImageInfo(200, 150)); surface.Canvas.DrawPicture(picture);
            using var image = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(image); Check(bitmap.GetPixel(100, 75).Alpha > 250);
        });
        test("Geometry gestures reuse decoded sources development shaders curves and brushes", GeometryPerformance);
        Directory.CreateDirectory("artifacts/engine"); File.WriteAllBytes("artifacts/engine/architecture-grid.png", GridFixture().Original);
        File.WriteAllBytes("artifacts/engine/white-balance.png", Solid(new SKColor(132, 128, 124), 300, 200).Original);
    }
    private static void GeometryPerformance()
    {
        var p = Tiny(); p.State = new() { Develop = new() { Exposure = .2f }, Optics = new(30) };
        using var renderer = new PhotoRenderer(); using var surface = SKSurface.Create(new SKImageInfo(96, 64));
        renderer.Draw(surface.Canvas, p, SKRect.Create(96, 64)); var before = renderer.Statistics; var presentation = renderer.Presentation; var curves = renderer.CurveLookupBuilds; var brushes = renderer.BrushStatistics;
        var watch = System.Diagnostics.Stopwatch.StartNew(); var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            p.State = p.State with { Geometry = new() { Rotate = i % 31, Vertical = i % 80, ConstrainCrop = true } };
            renderer.Draw(surface.Canvas, p, SKRect.Create(96, 64));
        }
        watch.Stop(); var after = renderer.Statistics;
        Check(after.ImageDecodes == before.ImageDecodes && after.ShaderBuilds == before.ShaderBuilds && renderer.CurveLookupBuilds == curves);
        Check(renderer.Presentation.ShaderBuilds == presentation.ShaderBuilds && renderer.BrushStatistics.DabsRasterized == brushes.DabsRasterized);
        Directory.CreateDirectory("artifacts/engine");
        File.WriteAllText("artifacts/engine/geometry-performance.json", JsonSerializer.Serialize(new
        {
            gestures = 100, before, after, presentationBefore = presentation, presentationAfter = renderer.Presentation,
            elapsedMilliseconds = watch.Elapsed.TotalMilliseconds, managedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated,
            scope = "100 warmed geometry updates and raster draws at 96x64. CPU time and work counters, not hardware GPU timing."
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static PhotoDocument GridFixture()
    {
        using var surface = SKSurface.Create(new SKImageInfo(640, 480)); surface.Canvas.Clear(new SKColor(194, 190, 180));
        using var paint = new SKPaint { Color = new SKColor(32, 46, 66) };
        for (var x = 0; x < 640; x += 40) surface.Canvas.DrawRect(x, 0, 5, 480, paint);
        for (var y = 0; y < 480; y += 40) surface.Canvas.DrawRect(0, y, 640, 5, paint);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return LightSpace.Imaging.PhotoCodec.Import("architecture-grid.png", data.ToArray());
    }
}
