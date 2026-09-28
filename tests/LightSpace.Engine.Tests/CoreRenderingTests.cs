using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class CoreRenderingTests
{
    public static void Register(Action<string, Action> Test)
    {
        Test("Clamp non-finite adjustments", () => Check(new DevelopSettings { Exposure = float.NaN, Contrast = float.PositiveInfinity }.Normalize().Exposure == 0));
        Test("Clamp adjustment range", () => Check(new DevelopSettings { Exposure = 90, Saturation = -400 }.Normalize() is { Exposure: 5, Saturation: -100 }));
        Test("Named adjustment accessors", () => { foreach (var name in new[] { "Exposure", "Contrast", "Highlights", "Shadows", "Whites", "Blacks", "Temperature", "Tint", "Vibrance", "Saturation", "Texture", "Clarity", "Dehaze", "Vignette", "Grain", "Sharpening", "NoiseReduction" }) Check(new DevelopSettings().Set(name, 1).Get(name) == 1); });
        Test("Mixer pads to eight bands", () => Check(new DevelopSettings { Mixer = [] }.Normalize().Mixer.Length == 8));
        Test("Default curve is identity", () => { for (var i = 0; i <= 100; i++) Check(Math.Abs(new ToneCurve().Evaluate(i / 100f) - i / 100f) < .0001); });
        Test("Curve endpoints clamp", () => Check(new ToneCurve(-1, .2f, .5f, .8f, 3).Normalize() is { Black: 0, White: 1 }));
        Test("Crop cannot be empty", () => Check(new CropSettings(.9f, .9f, .1f, .1f).Normalize().Width > .009));
        Test("Negative rotation normalizes", () => Check(new CropSettings(QuarterTurns: -1).Normalize().QuarterTurns == 3));
        Test("Rotation swaps dimensions", () => Check(new CropSettings(QuarterTurns: 1).OutputSize(96, 64) == (64, 96)));
        Test("Radial mask is localized", () => { var m = new LocalMask(); Check(m.Weight(.5f, .5f) > .99 && m.Weight(0, 0) == 0); });
        Test("Inverted mask complements", () => { var m = new LocalMask(); var n = m with { Inverted = true }; Check(Math.Abs(m.Weight(.6f, .6f) + n.Weight(.6f, .6f) - 1) < .0001); });
        Test("Linear mask spans image", () => { var m = new LocalMask { Kind = MaskKind.Linear }; Check(m.Weight(.5f, 0) == 0 && m.Weight(.5f, 1) == 1); });
        Test("Mask safety limit", () => Check(new PhotoState { Masks = Enumerable.Range(0, 12).Select(_ => new LocalMask()).ToArray() }.Normalize().Masks.Length == 8));
        Test("Keywords normalized", () => Check(new PhotoState { Keywords = [" Lake ", "lake", ""] }.Normalize().Keywords.Length == 1));
        Test("Catalog roundtrip preserves originals", () => { var c = Catalog(); var d = CatalogSerializer.Deserialize(CatalogSerializer.Serialize(c)); Check(d.Photos[0].Original.SequenceEqual(c.Photos[0].Original)); });
        Test("Unknown catalog schema rejected", () => Throws(() => CatalogSerializer.Deserialize("{\"SchemaVersion\":987}")));
        Test("Duplicate photo identities rejected", () => { var c = Catalog(); c.Photos[1].Id = c.Photos[0].Id; Throws(() => CatalogSerializer.Deserialize(CatalogSerializer.Serialize(c))); });
        Test("Orphan album entries removed", () => { var c = Catalog(); c.Albums.Add(new() { Photos = [Guid.NewGuid()] }); Check(CatalogSerializer.Deserialize(CatalogSerializer.Serialize(c)).Albums[0].Photos.Count == 0); });
        Test("Search is case-insensitive", () => Check(new PhotoQuery("PHOTO 1").Execute(Catalog()).Count == 1));
        Test("Rating and flag filters combine", () => { var c = Catalog(); c.Photos[0].State = new() { Rating = 5, Flag = PhotoFlag.Pick }; Check(new PhotoQuery(MinimumRating: 4, Flag: PhotoFlag.Pick).Execute(c).Count == 1); });
        Test("Undo and redo preserve source", () => { var s = new EditorSession(Catalog()); var bytes = s.Active!.Original; s.Edit("Exposure", p => p with { Develop = p.Develop with { Exposure = 1 } }); s.Undo(); Check(s.Active.State.Develop.Exposure == 0); s.Redo(); Check(s.Active.State.Develop.Exposure == 1 && ReferenceEquals(bytes, s.Active.Original)); });
        Test("Slider gesture coalesces", () => { var s = new EditorSession(Catalog()); for (var i = 0; i < 20; i++) s.Preview(p => p with { Develop = p.Develop with { Exposure = i / 10f } }); s.CommitGesture("Exposure"); Check(s.History.Count == 1); s.Undo(); Check(s.Active!.State.Develop.Exposure == 0); });
        Test("Canceled gesture restores state", () => { var s = new EditorSession(Catalog()); s.Preview(p => p with { Rating = 5 }); s.CancelGesture(); Check(s.Active!.State.Rating == 0 && !s.CanUndo); });
        Test("Batch edit is one transaction", () => { var c = Catalog(); var s = new EditorSession(c); s.Selection.UnionWith(c.Photos.Select(p => p.Id)); s.Edit("Rating", p => p with { Rating = 5 }, true); Check(c.Photos.All(p => p.State.Rating == 5) && s.History.Count == 1); s.Undo(); Check(c.Photos.All(p => p.State.Rating == 0)); });
        Test("Versions retain settings", () => { var s = new EditorSession(Catalog()); s.SaveVersion("Original"); s.Edit("Edit", p => p with { Rating = 5 }); Check(s.Active!.Versions[0].State.Rating == 0); });
        Test("No-op edits do not add history", () => { var s = new EditorSession(Catalog()); s.Edit("Nothing", p => p); Check(!s.CanUndo); });
        Test("New edits invalidate redo", () => { var s = new EditorSession(Catalog()); s.Edit("One", p => p with { Rating = 1 }); s.Undo(); s.Edit("Two", p => p with { Rating = 2 }); Check(!s.CanRedo); });
        Test("Image safety rejects invalid bytes", () => Throws(() => PhotoCodec.Import("bad.raw", [1, 2, 3])));
        Test("SkSL development pipeline compiles", () => { using var renderer = new PhotoRenderer(); Check(renderer.CachedImages == 0); });
        Test("Neutral shader retains pixels", () => { using var r = new PhotoRenderer(); var p = Tiny(); using var src = SKBitmap.Decode(p.Original); using var dst = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var a = src.GetPixel(48, 32); var b = dst.GetPixel(48, 32); Check(Math.Abs(a.Red - b.Red) <= 2 && Math.Abs(a.Green - b.Green) <= 2 && Math.Abs(a.Blue - b.Blue) <= 2); });
        Test("Exposure changes image pixels", () => { using var r = new PhotoRenderer(); var p = Tiny(); using var before = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); p.State = new() { Develop = new() { Exposure = 1 } }; using var after = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(after.GetPixel(20, 20).Red > before.GetPixel(20, 20).Red + 15); });
        Test("Monochrome produces equal channels", () => { using var r = new PhotoRenderer(); var p = Tiny(); p.State = new() { Develop = new() { Monochrome = true } }; using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var v = b.GetPixel(30, 30); Check(Math.Abs(v.Red - v.Green) <= 1 && Math.Abs(v.Red - v.Blue) <= 1); });
        Test("Crop and rotation export dimensions", () => { using var r = new PhotoRenderer(); var p = Tiny(); p.State = new() { Crop = new(.25f, 0, .75f, 1, 1) }; using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(b.Width == 64 && b.Height == 48); });
        Test("Histogram counts samples", () => { using var r = new PhotoRenderer(); var h = r.CalculateHistogram(Tiny()); Check(h.Count == 192 * 128 && h.Red.Sum() == h.Count && h.Green.Sum() == h.Count); });
        Test("Auto exposure stays bounded", () => { using var r = new PhotoRenderer(); Check(Math.Abs(r.Auto(Tiny()).Exposure) <= 2); });
        Test("Localized mask changes center", () => { using var r = new PhotoRenderer(); var p = Tiny(); using var before = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); p.State = new() { Masks = [new LocalMask { Exposure = 1.5f }] }; using var after = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(after.GetPixel(48, 32).Red > before.GetPixel(48, 32).Red + 10); Check(Math.Abs(after.GetPixel(1, 1).Red - before.GetPixel(1, 1).Red) <= 2); });
        Test("Clone spot samples source", () => { using var r = new PhotoRenderer(); var p = Tiny(); using var before = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); p.State = new() { CloneSpots = [new(.75f, .5f, .25f, .5f, .1f)] }; using var after = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(after.GetPixel(72, 32).Red < before.GetPixel(72, 32).Red - 30); });
        Test("Preview cache is bounded", () => { using var r = new PhotoRenderer(); using var s = SKSurface.Create(new SKImageInfo(64, 64)); foreach (var p in Catalog(7).Photos) r.Draw(s.Canvas, p, SKRect.Create(64, 64)); Check(r.CachedImages <= 5); });
        Test("Source transform is invertible", () => { for (var q = 0; q < 4; q++) { var m = PhotoTransform.SourceToView(new(.2f, .1f, .8f, .9f, q, true, false), 96, 64, SKRect.Create(20, 30, 600, 400)); Check(m.TryInvert(out var inverse)); var p = new SKPoint(40, 30); var restored = inverse.MapPoint(m.MapPoint(p)); Check(Math.Abs(restored.X - p.X) < .001 && Math.Abs(restored.Y - p.Y) < .001); } });
        Test("Generated sample decodes", () => { var p = PhotoCodec.Import("sample.png", SamplePhotos.Create(0)); Check(p.Width == 1440 && p.Height == 960); });
        Test("Preview shader invalidates on replacement state at the same revision", () =>
        {
            using var r = new PhotoRenderer(); var p = Tiny(); using var s = SKSurface.Create(new SKImageInfo(96, 64)); r.Draw(s.Canvas, p, SKRect.Create(96, 64));
            using var first = s.Snapshot(); using var before = SKBitmap.FromImage(first); p.State = p.State with { Develop = new() { Exposure = 1 } }; r.Draw(s.Canvas, p, SKRect.Create(96, 64));
            using var second = s.Snapshot(); using var after = SKBitmap.FromImage(second); Check(after.GetPixel(20, 20).Red > before.GetPixel(20, 20).Red + 15);
        });
        Test("Preview cache invalidates on replacement original at the same identity", () =>
        {
            using var r = new PhotoRenderer(); var p = Tiny(); using var s = SKSurface.Create(new SKImageInfo(96, 64)); r.Draw(s.Canvas, p, SKRect.Create(96, 64));
            p.Original = Solid(SKColors.Blue).Original; r.Draw(s.Canvas, p, SKRect.Create(96, 64)); using var snapshot = s.Snapshot(); using var after = SKBitmap.FromImage(snapshot);
            Check(after.GetPixel(20, 20).Blue > 250 && after.GetPixel(20, 20).Red < 3);
        });
    }
}
