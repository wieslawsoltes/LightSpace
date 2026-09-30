using System.Diagnostics;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class SurveyTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Survey pages preserve candidate order and deduplicate identities", () =>
        {
            var ids = Enumerable.Range(0, 29).Select(_ => Guid.NewGuid()).ToArray(); var s = new SurveySelection();
            s.Open(ids.Concat(ids.Take(2)), ids[13]);
            Check(s.Total == 29 && s.Page == 1 && s.PageCount == 3 && s.ActiveId == ids[13]);
            Check(s.VisibleIds.SequenceEqual(ids.Skip(12).Take(12)));
            s.MovePage(99); Check(s.VisibleIds.SequenceEqual(ids.Skip(24)) && s.ActiveId == ids[24]);
            s.MovePage(-99); Check(s.Page == 0 && s.ActiveId == ids[0]);
        });
        test("Survey exclusions reflow without changing the editing catalog", () =>
        {
            var editor = new EditorSession(Catalog(3)); var source = editor.Catalog.Photos.Select(p => p.Id).ToArray();
            var s = new SurveySelection(); s.Open(source); s.Exclude(source[0]);
            Check(s.Excluded == 1 && s.ActiveId == source[1] && editor.Revision == 0 && editor.Catalog.Photos.Count == 3);
            s.RestoreAll(); Check(s.VisibleIds.SequenceEqual(source) && s.ActiveId == source[1]);
        });
        test("Survey empty exclusions can restore every candidate", () =>
        {
            var s = new SurveySelection(); var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }; s.Open(ids);
            s.ExcludeWhere(_ => true); Check(s.Remaining == 0 && s.ActiveId == Guid.Empty && s.VisibleIds.Count == 0 && s.PageCount == 1);
            s.MoveActive(1); s.MovePage(1); s.RestoreAll(); Check(s.Remaining == 2 && s.ActiveId == ids[0]);
        });
        test("Survey predicate failure cannot partially exclude candidates", () =>
        {
            var s = new SurveySelection(); var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }; s.Open(ids);
            try { s.ExcludeWhere(id => id == ids[0] ? true : throw new ArithmeticException()); }
            catch (ArithmeticException) { }
            Check(s.Remaining == 2 && s.Excluded == 0);
        });
        test("Survey invalid open retains the previous candidates", () =>
        {
            var s = new SurveySelection(); var id = Guid.NewGuid(); s.Open([id]);
            try { s.Open([Guid.Empty]); throw new Exception("Accepted empty identity"); } catch (ArgumentException) { }
            Check(s.ActiveId == id && s.Total == 1);
            try { s.Open(Enumerable.Repeat(id, 5001)); throw new Exception("Accepted oversized candidates"); } catch (ArgumentException) { }
            Check(s.Total == 1);
        });
        test("Survey navigation clamps without spurious notifications", () =>
        {
            var s = new SurveySelection(); var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }; s.Open(ids);
            var changes = 0; s.Changed += () => changes++;
            s.MoveActive(-1); s.Exclude(Guid.NewGuid()); s.RestoreAll(); Check(changes == 0);
            s.MoveActive(int.MaxValue); Check(s.ActiveId == ids[1] && changes == 1);
            s.MoveActive(int.MinValue); Check(s.ActiveId == ids[0] && changes == 2);
        });
        test("Activating a culling candidate preserves multi-selection and revision", () =>
        {
            var s = new EditorSession(Catalog(3)); s.Selection.UnionWith(s.Catalog.Photos.Select(p => p.Id));
            s.ActivatePhoto(s.Catalog.Photos[1].Id);
            Check(s.Selection.Count == 3 && s.Revision == 0 && !s.CanUndo && s.Active!.Id == s.Catalog.Photos[1].Id);
        });
        test("Survey rating changes exactly one selected photo and undoes", () =>
        {
            var s = new EditorSession(Catalog(3)); s.Selection.UnionWith(s.Catalog.Photos.Select(p => p.Id));
            var photo = s.Catalog.Photos[1]; s.EditPhoto(photo.Id, "Rate candidate", state => state with { Rating = 5 });
            Check(s.Revision == 1 && s.History.Count == 1 && s.Catalog.Photos.Count(p => p.State.Rating == 5) == 1 && s.Selection.Count == 3);
            s.Undo(); Check(photo.State.Rating == 0); s.Redo(); Check(photo.State.Rating == 5);
        });
        test("Missing culling target rejects edits without committing a preview", () =>
        {
            var s = new EditorSession(Catalog()); s.Preview(state => state with { Rating = 2 });
            try { s.EditPhoto(Guid.NewGuid(), "Invalid", _ => new()); throw new Exception("Accepted missing photo"); } catch (InvalidOperationException) { }
            Check(s.Revision == 0 && s.HasActiveGesture); s.CancelGesture();
        });
        test("Survey layout is deterministic, bounded and nonoverlapping", () =>
        {
            var random = new Random(438);
            for (var n = 1; n <= 12; n++)
            foreach (var size in new[] { (100d, 80d), (480d, 800d), (1000d, 600d), (2000d, 280d) })
            {
                var images = Enumerable.Range(0, n).Select(_ => new SurveyImage(Guid.NewGuid(), .1 + random.NextDouble() * 10)).ToArray();
                var tiles = SurveyLayout.Arrange(images, size.Item1, size.Item2);
                Check(tiles.Length == n && tiles.SequenceEqual(SurveyLayout.Arrange(images, size.Item1, size.Item2)));
                for (var i = 0; i < n; i++)
                {
                    var b = tiles[i].Bounds; Check(b.X >= -1e-6 && b.Y >= -1e-6 && b.Right <= size.Item1 + 1e-6 && b.Bottom <= size.Item2 + 1e-6 && b.Width > 0 && b.Height > 0);
                    Check(tiles[i].ImageBounds.Bottom <= b.Bottom && tiles[i].Id == images[i].Id);
                    for (var j = i + 1; j < n; j++)
                    {
                        var a = tiles[j].Bounds;
                        Check(Math.Min(b.Right, a.Right) - Math.Max(b.X, a.X) <= 1e-6 || Math.Min(b.Bottom, a.Bottom) - Math.Max(b.Y, a.Y) <= 1e-6);
                    }
                }
            }
        });
        test("Survey layout rejects invalid input and accepts empty viewports", () =>
        {
            Check(SurveyLayout.Arrange([], 0, 0).Length == 0);
            foreach (var aspect in new[] { double.NaN, double.PositiveInfinity, -1d, 0 })
            {
                try { SurveyLayout.Arrange([new(Guid.NewGuid(), aspect)], 100, 100); throw new Exception("Accepted invalid aspect"); }
                catch (ArgumentException) { }
            }
        });
        test("Survey preparation shares source data and retains independent processing", () =>
        {
            var p = Tiny(); var reference = ReferencePhotoSnapshot.Capture(p); p.State = new() { Develop = new() { Exposure = 1 } };
            using var r = new PhotoRenderer(1024, 12, 48L * 1024 * 1024);
            r.Prepare(p); r.Prepare(reference.Photo); Check(r.Statistics.ImageDecodes == 1 && r.Statistics.DrawCalls == 0);
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            r.Draw(surface.Canvas, p, SKRect.Create(96, 64)); using var a = surface.Snapshot(); using var active = SKBitmap.FromImage(a);
            r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64)); using var b = surface.Snapshot(); using var original = SKBitmap.FromImage(b);
            Check(active.GetPixel(20, 20).Red > original.GetPixel(20, 20).Red + 15);
        });
        test("Twelve-photo warm survey avoids decode and shader-cache thrashing", () =>
        {
            var photos = Catalog(12).Photos;
            foreach (var p in photos) p.State = new() { Develop = new() { Exposure = .4f } };
            using var r = new PhotoRenderer(1024, 12, 48L * 1024 * 1024);
            foreach (var p in photos) r.Prepare(p);
            var before = r.Statistics; var clock = Stopwatch.StartNew();
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            for (var pass = 0; pass < 20; pass++) foreach (var p in photos)
            { p.State = p.State with { Rating = pass % 6 }; r.Draw(surface.Canvas, p, SKRect.Create(96, 64)); }
            clock.Stop(); var after = r.Statistics;
            Check(before.CachedSources == 12 && after.ImageDecodes == before.ImageDecodes && after.ShaderBuilds == before.ShaderBuilds);
            foreach (var p in photos) r.ReleasePhoto(p.Id); Check(r.Statistics.CachedBytes == 0);
            Directory.CreateDirectory("artifacts/engine");
            File.WriteAllText("artifacts/engine/survey-performance.json", JsonSerializer.Serialize(new
            { scope = "240 warm 96x64 CPU/raster draws across twelve sources; not GPU timings or full-size decode latency", before, after, elapsedMilliseconds = clock.Elapsed.TotalMilliseconds }));
        });
    }
}
