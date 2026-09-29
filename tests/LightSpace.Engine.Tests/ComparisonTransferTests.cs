using System.Diagnostics;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class ComparisonTransferTests
{
    public static void Register(Action<string, Action> test)
    {
        var full = new PhotoState
        {
            Develop = new() { Exposure = 1, Contrast = 18, Highlights = -20, Shadows = 25, Whites = 8, Blacks = -9,
                Temperature = 12, Tint = -8, Vibrance = 20, Saturation = 15, Monochrome = true,
                Curve = new(.02f, .2f, .5f, .8f, .99f), Channels = new() { Blue = new() { Points = [new(0, 0), new(.5f, .7f), new(1, 1)] } },
                Mixer = Enumerable.Range(0, 8).Select(_ => new ColorBand(8, 9, 10)).ToArray(), Grading = new() { Global = new(45, 12) },
                Texture = 15, Clarity = 20, Dehaze = 5, Vignette = -15, Grain = 8, Sharpening = 40, NoiseReduction = 12 },
            Crop = new(.1f, .1f, .9f, .9f, 1), Geometry = new() { Rotate = 10 }, Optics = new(12, 5, 2, -2),
            Masks = [new() { Exposure = 1 }], CloneSpots = [new(.5f, .5f, .2f, .2f)],
            Rating = 5, Caption = "source", Keywords = ["source"]
        }.Normalize();
        foreach (var group in Enum.GetValues<EditSettingsGroup>().Where(g => g != 0 && ((int)g & ((int)g - 1)) == 0))
        {
            test($"Selective transfer isolates {group}", () =>
            {
                var target = new PhotoState { Rating = 2, Flag = PhotoFlag.Pick, Label = "Green", Caption = "retain", Keywords = ["target"] };
                var one = new EditSettingsTransfer(full, group).Apply(target);
                Check(!PhotoStateEquality.Pixels(target, one), "Selected group must actually change its fields");
                var restored = new EditSettingsTransfer(target, group).Apply(one);
                Check(PhotoStateEquality.All(target, restored));
                Check(one.Rating == 2 && one.Flag == PhotoFlag.Pick && one.Caption == "retain" && ReferenceEquals(one.Keywords, target.Keywords));
                Check(PhotoStateEquality.Pixels(new EditSettingsTransfer(full, EditSettingsGroup.All & ~group).Apply(one), full));
            });
        }
        test("Selective transfer None and equal values preserve identity", () =>
        {
            Check(ReferenceEquals(full, new EditSettingsTransfer(new(), EditSettingsGroup.None).Apply(full)));
            Check(ReferenceEquals(full, new EditSettingsTransfer(full, EditSettingsGroup.All).Apply(full)));
        });
        test("Selective transfer rejects unknown flag bits", () =>
        {
            try { _ = new EditSettingsTransfer(full, (EditSettingsGroup)(1 << 25)); throw new Exception("Accepted unknown flags"); }
            catch (ArgumentOutOfRangeException) { }
        });
        test("Selective transfer is one transaction for deduplicated targets", () =>
        {
            var s = new EditorSession(Catalog(3)); var ids = s.Catalog.Photos.Select(p => p.Id).ToArray();
            s.ApplySettings(new(full, EditSettingsGroup.Light), ids.Concat(ids));
            Check(s.Revision == 1 && s.History.Count == 1 && s.Catalog.Photos.All(p => p.State.Develop.Exposure == 1 && p.State.Develop.Temperature == 0));
            s.Undo(); Check(s.Catalog.Photos.All(p => p.State.Develop.Exposure == 0)); s.Redo(); Check(s.Catalog.Photos.All(p => p.State.Develop.Exposure == 1));
        });
        test("Missing transfer targets reject the entire batch", () =>
        {
            var s = new EditorSession(Catalog());
            try { s.ApplySettings(new(full, EditSettingsGroup.All), [s.Active!.Id, Guid.NewGuid()]); throw new Exception("Accepted missing photo"); }
            catch (InvalidOperationException) { }
            Check(s.Revision == 0 && !s.CanUndo && s.Active!.State.Develop.Exposure == 0);
        });
        test("No-op transfer leaves revision and recovery unchanged", () =>
        {
            var s = new EditorSession(Catalog()); s.ApplySettings(new(new(), EditSettingsGroup.All), s.Catalog.Photos.Select(p => p.Id));
            Check(s.Revision == 0 && !s.CanUndo);
        });
        test("Sync captures the finalized active gesture", () =>
        {
            var s = new EditorSession(Catalog()); s.Selection.UnionWith(s.Catalog.Photos.Select(p => p.Id));
            s.Preview(p => p with { Develop = p.Develop with { Exposure = 2 } }); s.SyncSelected(EditSettingsGroup.Light);
            Check(!s.HasActiveGesture && s.Catalog.Photos.All(p => p.State.Develop.Exposure == 2));
        });
        test("Reference snapshot freezes edits without duplicating original bytes", () =>
        {
            var p = Tiny(); p.State = full; var reference = ReferencePhotoSnapshot.Capture(p);
            p.State = new(); Check(reference.SourcePhotoId == p.Id && reference.Photo.Id != p.Id);
            Check(ReferenceEquals(reference.Photo.Original, p.Original) && PhotoStateEquality.All(reference.Photo.State, full));
        });
        test("Reference and active views share a decode but not shader snapshots", () =>
        {
            using var r = new PhotoRenderer(); var p = Tiny(); var reference = ReferencePhotoSnapshot.Capture(p);
            p.State = new() { Develop = new() { Exposure = 1 } };
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            r.Draw(surface.Canvas, p, SKRect.Create(96, 64)); using var a = surface.Snapshot(); using var edited = SKBitmap.FromImage(a);
            r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64)); using var b = surface.Snapshot(); using var frozen = SKBitmap.FromImage(b);
            Check(edited.GetPixel(20, 20).Red > frozen.GetPixel(20, 20).Red + 15);
            Check(r.Statistics.ImageDecodes == 1 && r.Statistics.CachedSources == 1 && r.Statistics.CachedImages == 2 && r.Statistics.CachedBytes == 96 * 64 * 4);
            r.ReleasePhoto(p.Id); r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64));
            Check(r.Statistics.ImageDecodes == 1); r.ReleasePhoto(reference.Photo.Id); Check(r.Statistics.CachedBytes == 0);
        });
        test("Single-slot eviction transfers shared source ownership safely", () =>
        {
            using var r = new PhotoRenderer(maxImages: 1); var p = Tiny(); var reference = ReferencePhotoSnapshot.Capture(p);
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            for (var i = 0; i < 20; i++) { r.Draw(surface.Canvas, i % 2 == 0 ? p : reference.Photo, SKRect.Create(96, 64)); GC.Collect(); GC.WaitForPendingFinalizers(); }
            Check(r.Statistics.ImageDecodes == 1 && r.Statistics.CachedImages == 1 && r.Statistics.CachedSources == 1);
            r.Clear(); r.Clear(); Check(r.Statistics.CachedBytes == 0);
        });
        test("Replacing an original does not corrupt another reference owner", () =>
        {
            using var r = new PhotoRenderer(); var p = Tiny(); var reference = ReferencePhotoSnapshot.Capture(p);
            using var surface = SKSurface.Create(new SKImageInfo(96, 64)); r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64));
            p.Original = Solid(SKColors.Blue).Original; r.Draw(surface.Canvas, p, SKRect.Create(96, 64));
            r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64)); using var image = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(image);
            Check(bitmap.GetPixel(20, 20).Red > 50 && r.Statistics.ImageDecodes == 2);
        });
        test("Paired warm redraws avoid decoding and shader rebuilding", () =>
        {
            using var r = new PhotoRenderer(); var p = Tiny(); p.State = full; var reference = ReferencePhotoSnapshot.Capture(p);
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            r.Draw(surface.Canvas, p, SKRect.Create(96, 64)); r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64));
            var before = r.Statistics; var timer = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++) { r.Draw(surface.Canvas, p, SKRect.Create(96, 64)); r.Draw(surface.Canvas, reference.Photo, SKRect.Create(96, 64)); }
            timer.Stop(); var after = r.Statistics;
            Check(before.ImageDecodes == 1 && after.ImageDecodes == before.ImageDecodes && after.ShaderBuilds == before.ShaderBuilds);
            Directory.CreateDirectory("artifacts/engine");
            File.WriteAllText("artifacts/engine/comparison-performance.json", JsonSerializer.Serialize(new
            { scope = "200 warm raster draws of active/reference sharing one source; CPU elapsed time, not GPU timings", before, after, elapsedMilliseconds = timer.Elapsed.TotalMilliseconds }));
        });
    }
}
