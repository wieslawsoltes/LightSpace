using System.Diagnostics;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class CropInteractionTests
{
    private static readonly CropHandle[] Handles = [CropHandle.TopLeft, CropHandle.Top, CropHandle.TopRight, CropHandle.Right,
        CropHandle.BottomRight, CropHandle.Bottom, CropHandle.BottomLeft, CropHandle.Left];
    private static void Near(double a, double b, double tolerance = 1e-6) => Check(Math.Abs(a - b) <= tolerance, $"{a} != {b}");
    private static void Inside(RectD rect)
    {
        Check(rect.X >= -1e-9 && rect.Y >= -1e-9 && rect.Right <= 1 + 1e-9 && rect.Bottom <= 1 + 1e-9);
        Check(rect.Width >= .01 - 1e-6 && rect.Height >= .01 - 1e-6);
    }
    public static void Register(Action<string, Action> test)
    {
        foreach (var handle in Handles)
            test($"Locked {handle} preserves aspect and bounds under arbitrary pointer excursions", () =>
            {
                var random = new Random(20260930 + (int)handle);
                for (var i = 0; i < 600; i++)
                {
                    var x = random.NextDouble() * .6; var y = random.NextDouble() * .6;
                    var crop = new CropSettings((float)x, (float)y, (float)(x + .03 + random.NextDouble() * (1 - x - .03)),
                        (float)(y + .03 + random.NextDouble() * (1 - y - .03)));
                    var start = CropGeometry.Bounds(crop); var press = new PointD(.5, .5);
                    var gesture = new CropGesture(crop, handle, press, 6000, 4000, true, i % 2 == 0);
                    var result = gesture.Evaluate(new(random.NextDouble() * 6 - 3, random.NextDouble() * 6 - 3));
                    Inside(result); Near(result.Width / result.Height, start.Width / start.Height, 1e-9);
                    var converted = CropGeometry.Apply(crop, result); Inside(CropGeometry.Bounds(converted));
                    Near(CropGeometry.OutputAspect(converted, 6000, 4000), CropGeometry.OutputAspect(crop, 6000, 4000), .001);
                }
            });
        test("Crop corner resizing holds the opposite anchor and centered resize holds center", () =>
        {
            var crop = new CropSettings(.2f, .2f, .8f, .8f); var start = CropGeometry.Bounds(crop);
            var ordinary = new CropGesture(crop, CropHandle.BottomRight, new(.8, .8), 6000, 4000, true).Evaluate(new(.6, .5));
            Near(ordinary.X, start.X); Near(ordinary.Y, start.Y);
            var centered = new CropGesture(crop, CropHandle.BottomRight, new(.8, .8), 6000, 4000, true, true).Evaluate(new(.6, .5));
            Near(centered.X + centered.Width / 2, start.X + start.Width / 2);
            Near(centered.Y + centered.Height / 2, start.Y + start.Height / 2);
        });
        test("Crop edge resizing preserves opposing edge and secondary center", () =>
        {
            var crop = new CropSettings(.2f, .2f, .8f, .8f); var start = CropGeometry.Bounds(crop);
            var right = new CropGesture(crop, CropHandle.Right, new(.8, .5), 6000, 4000, true).Evaluate(new(.65, .9));
            Near(right.X, start.X); Near(right.Y + right.Height / 2, start.Y + start.Height / 2);
            Near(right.Width, start.Width - .15);
            var top = new CropGesture(crop, CropHandle.Top, new(.5, .2), 6000, 4000, true).Evaluate(new(.95, .35));
            Near(top.Bottom, start.Bottom); Near(top.X + top.Width / 2, start.X + start.Width / 2);
        });
        test("Crop constraints are pointer-density independent and do not snap on press", () =>
        {
            var crop = new CropSettings(.2f, .15f, .8f, .85f); var press = new PointD(.79, .84);
            var gesture = new CropGesture(crop, CropHandle.BottomRight, press, 6000, 4000, true);
            Check(gesture.Evaluate(press) == CropGeometry.Bounds(crop));
            var end = new PointD(.65, .7); var expected = gesture.Evaluate(end);
            for (var i = 0; i < 100; i++) _ = gesture.Evaluate(new(.9 - i / 400d, .95 - i / 400d));
            Check(gesture.Evaluate(end) == expected);
            Check(ReferenceEquals(crop, CropGeometry.Apply(crop, CropGeometry.Bounds(crop))));
        });
        test("Crop movement clamps translation without changing dimensions", () =>
        {
            var crop = new CropSettings(.2f, .3f, .7f, .8f); var start = CropGeometry.Bounds(crop);
            var result = new CropGesture(crop, CropHandle.Move, new(.4, .5), 6000, 4000, true).Evaluate(new(9, -8));
            Near(result.Right, 1); Near(result.Y, 0); Near(result.Width, start.Width); Near(result.Height, start.Height);
        });
        test("Free resizing is independent in both dimensions", () =>
        {
            var crop = new CropSettings(.2f, .2f, .8f, .8f);
            var result = new CropGesture(crop, CropHandle.BottomRight, new(.8, .8), 6000, 4000).Evaluate(new(.6, .75));
            Near(result.Width, (double)crop.Right - crop.Left - .2); Near(result.Height, (double)crop.Bottom - crop.Top - .05);
        });
        test("Creating constrained crops handles all quadrants, centers and unusable border anchors", () =>
        {
            foreach (var x in new[] { -.8, .8 }) foreach (var y in new[] { -.6, .6 }) foreach (var center in new[] { false, true })
            {
                var result = new CropGesture(new(), CropHandle.Create, new(.5, .5), 6000, 4000, true, center).Evaluate(new(.5 + x, .5 + y));
                Inside(result); Near(result.Width / result.Height, 1);
                if (center) { Near(result.X + result.Width / 2, .5); Near(result.Y + result.Height / 2, .5); }
            }
            var edge = new CropGesture(new(), CropHandle.Create, new(.9999, .9999), 6000, 4000, true);
            Check(edge.Evaluate(new(1, 1)) == new RectD(0, 0, 1, 1));
        });
        test("Crop aspect presets describe output dimensions after every quarter-turn and flip", () =>
        {
            for (var q = 0; q < 4; q++) foreach (var flip in new[] { false, true }) foreach (var ratio in new[] { .8, 1, 1.5, 16d / 9 })
            {
                var original = new CropSettings(.2f, .1f, .8f, .9f, q, flip, !flip);
                var crop = CropGeometry.WithAspect(original, ratio, 6000, 4000);
                Near(CropGeometry.OutputAspect(crop, 6000, 4000), ratio);
                Check(crop.QuarterTurns == q && crop.FlipX == flip && crop.FlipY == !flip);
                Inside(CropGeometry.Bounds(crop));
                var (w, h) = crop.OutputSize(6000, 4000); Check(Math.Abs(w - ratio * h) <= 1.5);
            }
        });
        test("Crop aspect validation rejects non-finite, negative and impossible ratios", () =>
        {
            foreach (var ratio in new[] { double.NaN, double.PositiveInfinity, 0, -1, 1e-10, 1e10 })
            {
                try { CropGeometry.WithAspect(new(), ratio, 6000, 4000); throw new InvalidOperationException("Accepted invalid ratio"); }
                catch (ArgumentOutOfRangeException) { }
            }
            var gesture = new CropGesture(new(), CropHandle.Right, new(.5, .5), 6000, 4000, true);
            Check(gesture.Evaluate(new(double.NaN, 0)) == new RectD(0, 0, 1, 1));
        });
        test("Crop aspect changes retain size when feasible and roundtrip without new schema fields", () =>
        {
            var original = new CropSettings(.3f, .3f, .7f, .7f); var box = CropGeometry.Bounds(original);
            var swapped = CropGeometry.WithAspect(original, 1 / CropGeometry.OutputAspect(original, 6000, 4000), 6000, 4000);
            var other = CropGeometry.Bounds(swapped); Near(box.Width * box.Height, other.Width * other.Height);
            var state = new PhotoState { Crop = swapped };
            Check(PhotoStateEquality.All(state, CatalogSerializer.DeserializeSettings(CatalogSerializer.SerializeSettings(state))));
            var session = new EditorSession(Catalog()); var photo = session.Active!;
            session.Preview(s => s with { Crop = swapped }); session.CancelGesture(); Check(photo.State.Crop == new CropSettings());
            session.Edit("Crop aspect", s => s with { Crop = swapped }); session.Undo(); Check(photo.State.Crop == new CropSettings());
            session.Redo(); Check(photo.State.Crop == swapped);
        });
        test("Crop-only drags reuse decoded sources, development and optical shaders", () =>
        {
            using var renderer = new PhotoRenderer(); var photo = Tiny();
            photo.State = new() { Develop = new() { Exposure = .5f }, Optics = new(8, 5, 0, 0), Geometry = new() { Rotate = 5 } };
            using var surface = SKSurface.Create(new SKImageInfo(96, 64)); renderer.Draw(surface.Canvas, photo, SKRect.Create(96, 64));
            var before = renderer.Statistics; var presentation = renderer.Presentation; var curves = renderer.CurveLookupBuilds;
            for (var i = 0; i < 80; i++)
            {
                photo.State = photo.State with { Crop = CropGeometry.WithAspect(new(), .8 + i / 80d, 96, 64) };
                renderer.Draw(surface.Canvas, photo, SKRect.Create(96, 64));
            }
            Check(renderer.Statistics.ImageDecodes == before.ImageDecodes && renderer.Statistics.ShaderBuilds == before.ShaderBuilds);
            Check(renderer.Presentation == presentation && renderer.CurveLookupBuilds == curves);
        });
        test("All crop guides have bounded finite geometry and reject short buffers", () =>
        {
            Span<CropGuideLine> lines = stackalloc CropGuideLine[CropGuides.MaximumLines]; var rect = new RectD(20, 30, 900, 600);
            foreach (var guide in Enum.GetValues<CropGuide>()) foreach (var reverse in new[] { false, true })
            {
                var count = CropGuides.Write(guide, rect, reverse, lines); Check(count <= CropGuides.MaximumLines);
                for (var i = 0; i < count; i++) { Check(rect.Contains(lines[i].Start)); Check(rect.Contains(lines[i].End)); }
                try { CropGuides.Write(guide, rect, reverse, Span<CropGuideLine>.Empty); Check(count == 0); }
                catch (ArgumentException) { Check(count > 0); }
            }
        });
        test("Golden triangle short guides meet the diagonal at right angles", () =>
        {
            Span<CropGuideLine> lines = stackalloc CropGuideLine[CropGuides.MaximumLines];
            CropGuides.Write(CropGuide.Triangle, new(0, 0, 900, 600), false, lines);
            var d = lines[0];
            for (var i = 1; i < 3; i++) Near((d.End.X - d.Start.X) * (lines[i].End.X - lines[i].Start.X)
                + (d.End.Y - d.Start.Y) * (lines[i].End.Y - lines[i].Start.Y), 0, 1e-6);
        });
        test("Warm crop evaluation and guide generation allocate no managed memory", () =>
        {
            var gesture = new CropGesture(new(.2f, .2f, .8f, .8f), CropHandle.BottomRight, new(.8, .8), 6000, 4000, true);
            Span<CropGuideLine> lines = stackalloc CropGuideLine[CropGuides.MaximumLines];
            for (var i = 0; i < 2000; i++) _ = CropGuides.Write(CropGuide.Grid, gesture.Evaluate(new(.5, .6)), false, lines);
            var timer = new Stopwatch(); var before = GC.GetAllocatedBytesForCurrentThread(); var sum = 0d; timer.Start();
            for (var i = 0; i < 100000; i++)
            {
                var rect = gesture.Evaluate(new(.4 + (i % 500) / 1000d, .7)); sum += rect.Width;
                _ = CropGuides.Write(CropGuide.Grid, rect, false, lines);
            }
            timer.Stop(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0 && sum > 0);
            Directory.CreateDirectory("artifacts/engine");
            File.WriteAllText("artifacts/engine/crop-performance.json", JsonSerializer.Serialize(new
            { iterations = 100000, managedBytes = allocated, cpuMilliseconds = timer.Elapsed.TotalMilliseconds, checksum = sum,
                scope = "Warm analytic crop evaluation plus caller-owned guide buffer; excludes immutable edit snapshots, UI, codec and GPU execution." }));
        });
    }
}
