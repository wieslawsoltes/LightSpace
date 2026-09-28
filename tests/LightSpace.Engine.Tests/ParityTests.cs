using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class ParityTests
{
    public static void Register(Action<string, Action> Test)
    {
        Test("Grading normalizes finite ranges", () => Check(new GradingTone(-30, 300, float.NaN).Normalize() is { Hue: 330, Saturation: 100, Luminance: 0 }));
        Test("Grading weights sum to one", () => { foreach (var balance in new[] { -100, 0, 100 }) foreach (var blend in new[] { 0, 50, 100 }) for (var i = 0; i <= 100; i++) { var w = new ColorGradingSettings { Balance = balance, Blending = blend }.Weights(i / 100f); Check(Math.Abs(w.X + w.Y + w.Z - 1) < .00001 && w.X >= 0 && w.Y >= 0 && w.Z >= 0); } });
        Test("Grading tint vector has zero luminance", () => { var tint = new GradingTone(40, 100).TintVector(); Check(Math.Abs(.2126 * tint.X + .7152 * tint.Y + .0722 * tint.Z) < .00001); });
        Test("Neutral grading is pixel identity through the shader", () =>
        {
            var p = Tiny(); using var image = PhotoCodec.Decode(p.Original); using var r = new PhotoRenderer(); using var shader = r.CreateShader(image, p.State);
            using var surface = SKSurface.Create(new SKImageInfo(96, 64)); using var paint = new SKPaint { Shader = shader }; surface.Canvas.DrawRect(0, 0, 96, 64, paint);
            using var output = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(output); using var original = SKBitmap.Decode(p.Original);
            for (var y = 0; y < 64; y += 8) for (var x = 0; x < 96; x += 8) { var a = bitmap.GetPixel(x, y); var b = original.GetPixel(x, y); Check(Math.Abs(a.Red - b.Red) < 3 && Math.Abs(a.Green - b.Green) < 3 && Math.Abs(a.Blue - b.Blue) < 3); }
        });
        Test("Global grading colorizes neutral pixels", () =>
        {
            var p = Solid(new(128, 128, 128)); p.State = new() { Develop = new() { Grading = new() { Global = new(0, 70) } } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var c = b.GetPixel(32, 32); Check(c.Red > c.Green + 15 && c.Red > c.Blue + 15);
        });
        Test("Grading luminance adjusts brightness", () =>
        {
            var p = Solid(new(100, 100, 100)); p.State = new() { Develop = new() { Grading = new() { Global = new(0, 0, 100) } } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(b.GetPixel(32, 32).Red > 130);
        });
        Test("Grading applies after monochrome", () =>
        {
            var p = Tiny(); p.State = new() { Develop = new() { Monochrome = true, Grading = new() { Global = new(210, 40) } } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var c = b.GetPixel(48, 32); Check(c.Blue > c.Red + 5);
        });
        Test("Shadow grading does not tint highlights", () =>
        {
            var p = Solid(new(230, 230, 230)); p.State = new() { Develop = new() { Grading = new() { Shadows = new(0, 100) } } };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var c = b.GetPixel(32, 32); Check(Math.Abs(c.Red - c.Green) <= 1);
        });
        Test("Grading roundtrip and undo retain all ranges", () =>
        {
            var s = new EditorSession(Catalog()); var grading = new ColorGradingSettings { Shadows = new(200, 10), Midtones = new(30, 20), Highlights = new(50, 30), Global = new(10, 4), Balance = 12, Blending = 73 };
            s.Edit("Grade", p => p with { Develop = p.Develop with { Grading = grading } }); var saved = CatalogSerializer.Deserialize(CatalogSerializer.Serialize(s.Catalog));
            Check(saved.Photos[0].State.Develop.Grading == grading); s.Undo(); Check(s.Active!.State.Develop.Grading.IsNeutral); s.Redo(); Check(s.Active.State.Develop.Grading == grading);
        });
        Test("Legacy schema migrates to the current explicit processing schema", () =>
        {
            var legacy = CatalogSerializer.Serialize(Catalog()).Replace($"\"SchemaVersion\":{CatalogDocument.CurrentSchemaVersion}", "\"SchemaVersion\":1");
            var migrated = CatalogSerializer.Deserialize(legacy); Check(migrated.SchemaVersion == CatalogDocument.CurrentSchemaVersion && migrated.Photos[0].State.Develop.Grading.IsNeutral);
        });
        Test("Rotated linear gradient reverses its normal", () => { var m = new LocalMask { Kind = MaskKind.Linear, Angle = 90 }; Check(m.Weight(.1f, .5f) > .99 && m.Weight(.9f, .5f) < .01); });
        Test("Radial rotation uses pixel-isotropic coordinates", () =>
        {
            var m = new LocalMask { RadiusX = .25f, RadiusY = .1f, Angle = 90, Feather = .2f }; var point = m.LocalToSource(.25f, 0, 2);
            Check(m.Weight((float)point.X, (float)point.Y, .5f, 2) > .99); Check(m.Weight(.7f, .5f, .5f, 2) < .01);
        });
        Test("Luminance range selects only source brightness", () =>
        {
            var m = new LocalMask { Kind = MaskKind.LuminanceRange, RangeMin = .3f, RangeMax = .6f, RangeSmoothness = .05f };
            Check(m.Weight(0, 0, .45f) == 1 && m.Weight(.5f, .5f, .1f) == 0 && m.Weight(.5f, .5f, .9f) == 0);
        });
        Test("Range edges feather without losing black or white endpoints", () =>
        {
            var m = new LocalMask { Kind = MaskKind.LuminanceRange, RangeMin = 0, RangeMax = 1 };
            Check(m.Weight(.5f, .5f, 0) == 1 && m.Weight(.5f, .5f, 1) == 1);
            m = m with { RangeMin = .3f, RangeSmoothness = .1f }; Check(Math.Abs(m.Weight(.5f, .5f, .25f) - .5f) < .00001);
        });
        Test("Range and radial selection intersect before inversion", () =>
        {
            var m = new LocalMask { RangeEnabled = true, RangeMin = .3f, RangeMax = .6f };
            Check(m.Weight(0, 0, .4f) == 0 && m.Weight(.5f, .5f, .4f) == 1 && m.Weight(.5f, .5f, .9f) == 0);
            Check((m with { Inverted = true }).Weight(0, 0, .4f) == 1);
        });
        Test("Disabled masks and zero amount produce no coverage", () => { Check(new LocalMask { Enabled = false, Inverted = true }.Weight(.5f, .5f) == 0); Check(new LocalMask { Opacity = 0 }.Weight(.5f, .5f) == 0); });
        Test("Mask normalization repairs reversed ranges", () => { var m = new LocalMask { RangeMin = .8f, RangeMax = .2f, Angle = -90 }.Normalize(); Check(m.RangeMin == .2f && m.RangeMax == .8f && m.Angle == 270); });
        Test("Rotated gradient CPU coverage matches shader pixels", () => CompareMask(new() { Kind = MaskKind.Linear, Angle = 37, Exposure = 1, RadiusY = .25f }));
        Test("Rotated radial CPU coverage matches shader pixels", () => CompareMask(new() { Angle = 28, Exposure = 1, RadiusX = .22f, RadiusY = .31f, Opacity = .6f }));
        Test("Range-mask CPU coverage matches shader pixels", () => CompareMask(new() { Kind = MaskKind.LuminanceRange, RangeMin = .45f, RangeMax = .8f, RangeSmoothness = .2f, Exposure = 1 }));
        Test("Local white balance affects selected pixels", () =>
        {
            var p = Solid(new(128, 128, 128)); p.State = new() { Masks = [new() { Exposure = 0, Temperature = 100 }] };
            using var r = new PhotoRenderer(); using var b = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); var c = b.GetPixel(48, 32); Check(c.Red > c.Blue + 15); Check(b.GetPixel(0, 0).Red == b.GetPixel(0, 0).Blue);
        });
        Test("Coverage overlay never contaminates export", () =>
        {
            var p = Solid(new(128, 128, 128)); p.State = new() { Masks = [new() { Exposure = 0 }] };
            using var r = new PhotoRenderer(); using var s = SKSurface.Create(new SKImageInfo(96, 64)); r.Draw(s.Canvas, p, SKRect.Create(96, 64), overlayMask: 0);
            using var preview = s.Snapshot(); using var overlaid = SKBitmap.FromImage(preview); Check(overlaid.GetPixel(48, 32).Red > overlaid.GetPixel(48, 32).Green + 30);
            using var exported = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png)); Check(Math.Abs(exported.GetPixel(48, 32).Red - 128) <= 2 && exported.GetPixel(48, 32).Red == exported.GetPixel(48, 32).Green);
        });
        Test("Metadata and mask names do not invalidate pixel equality", () =>
        {
            var a = new PhotoState { Masks = [new()] }; var b = a with { Rating = 4, Caption = "caption", Masks = [a.Masks[0] with { Name = "renamed" }] };
            Check(PhotoStateEquality.Pixels(a, b) && !PhotoStateEquality.All(a, b));
        });
        Test("Crop changes geometry without invalidating development shader", () => { var a = new PhotoState(); var b = a with { Crop = new(.2f, .2f, .8f, .8f) }; Check(PhotoStateEquality.Shader(a, b) && !PhotoStateEquality.Pixels(a, b)); });
        Test("Normalized snapshot collections retain reference identity", () =>
        {
            var state = new PhotoState { Masks = [new()], CloneSpots = [new(.5f, .5f, .2f, .2f)], Keywords = ["landscape"] }; var normalized = state.Normalize();
            Check(ReferenceEquals(normalized.Masks, state.Masks) && ReferenceEquals(normalized.CloneSpots, state.CloneSpots) && ReferenceEquals(normalized.Keywords, state.Keywords) && ReferenceEquals(normalized.Develop.Mixer, state.Develop.Mixer));
        });
        Test("Auto tone samples a bounded 96 by 64 image", () => { using var r = new PhotoRenderer(); r.Auto(Solid(SKColors.Gray, 1600, 1200)); Check(r.Statistics.AutoSamples == 6144); });
        Test("Thumbnail-resolution renderer retains only small decoded previews", () =>
        {
            using var r = new PhotoRenderer(384, 2, 8L * 1024 * 1024); using var s = SKSurface.Create(new SKImageInfo(240, 160)); var p = Solid(SKColors.Gray, 1600, 1200);
            r.Draw(s.Canvas, p, SKRect.Create(240, 160)); Check(r.Statistics.CachedBytes <= 384L * 384 * 4);
        });
    }
    private static void CompareMask(LocalMask mask)
    {
        var p = Solid(new(102, 102, 102), 120, 80); p.State = new() { Masks = [mask] };
        using var r = new PhotoRenderer(); using var image = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
        var linear = Math.Pow((.4 + .055) / 1.055, 2.4) * 2;
        var adjusted = 1.055 * Math.Pow(linear, 1 / 2.4) - .055;
        for (var y = 5; y < 80; y += 13) for (var x = 5; x < 120; x += 17)
        {
            var weight = mask.Weight((x + .5f) / 120, (y + .5f) / 80, .4f, 1.5f);
            var expected = 255 * (.4 + (adjusted - .4) * weight);
            Check(Math.Abs(image.GetPixel(x, y).Red - expected) < 3, $"Mask mismatch at {x},{y}: {image.GetPixel(x, y).Red} vs {expected:0.00}");
        }
    }
}
