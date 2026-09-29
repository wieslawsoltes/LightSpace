using LightSpace.Catalog;
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class ColorRangeTests
{
    private static ColorRangeSettings Red => new() { Enabled = true, Samples = [new(180 / 255f, 45 / 255f, 50 / 255f)], Tolerance = .04f, Smoothness = .08f };
    public static PhotoDocument Stripes()
    {
        using var surface = SKSurface.Create(new SKImageInfo(300, 180)); using var paint = new SKPaint();
        SKColor[] colors = [new(180, 45, 50), new(50, 170, 65), new(55, 70, 180)];
        for (var i = 0; i < 3; i++) { paint.Color = colors[i]; surface.Canvas.DrawRect(i * 100, 0, 100, 180, paint); }
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return PhotoCodec.Import("color-ranges.png", data.ToArray());
    }
    public static void Register(Action<string, Action> test)
    {
        test("Oklab primary-color reference vectors match", () =>
        {
            var r = ColorRangeSettings.ToOklab(1, 0, 0); var w = ColorRangeSettings.ToOklab(1, 1, 1);
            Check(MathF.Abs(r.X - .627955f) < .00001 && MathF.Abs(r.Y - .224863f) < .00001 && MathF.Abs(r.Z - .125846f) < .00001);
            Check(MathF.Abs(w.X - 1) < .00001 && MathF.Abs(w.Y) < .00001 && MathF.Abs(w.Z) < .00001);
        });
        test("Color samples normalize and clamp to five", () =>
        {
            var settings = new ColorRangeSettings { Tolerance = float.NaN, Smoothness = -1, Samples = Enumerable.Range(0, 9).Select(_ => new ColorSample(2, -1, float.NaN)).ToArray() }.Normalize();
            Check(settings.Samples.Length == 5 && settings.Tolerance == 0 && settings.Smoothness == .001f && settings.Samples[0].Red == 1 && settings.Samples[0].Green == 0);
            Check(ReferenceEquals(settings, settings.Normalize()));
        });
        test("Color selection is neutral when disabled and empty when awaiting samples", () =>
        {
            var source = new ColorSample(.4f, .5f, .6f); Check(new ColorRangeSettings().Weight(source) == 1);
            Check(new ColorRangeSettings { Enabled = true }.Weight(source) == 0);
            Check(new LocalMask { Kind = MaskKind.ColorRange }.Weight(.5f, .5f, source) == 0);
        });
        test("Color samples form a union of perceptual neighborhoods", () =>
        {
            var blue = new ColorSample(55 / 255f, 70 / 255f, 180 / 255f);
            var range = Red with { Samples = [.. Red.Samples, blue] };
            Check(range.Weight(blue) == 1 && range.Weight(Red.Samples[0]) == 1 && range.Weight(new(50 / 255f, 170 / 255f, 65 / 255f)) < .01f);
        });
        test("Color tolerance feathers continuously", () =>
        {
            var a = new ColorSample(.3f, .4f, .5f); var b = new ColorSample(.5f, .4f, .5f);
            var d = System.Numerics.Vector3.Distance(a.ToOklab(), b.ToOklab());
            var range = new ColorRangeSettings { Enabled = true, Samples = [a], Tolerance = d / 2, Smoothness = d };
            Check(MathF.Abs(range.Weight(b) - .5f) < .0001f);
        });
        test("Color restriction intersects spatial and luminance masks before inversion", () =>
        {
            var m = new LocalMask { ColorRange = Red, RangeEnabled = true, RangeMin = .1f, RangeMax = .8f, Opacity = .4f };
            var color = Red.Samples[0]; var normal = m.Weight(.5f, .5f, color);
            Check(MathF.Abs(normal - .4f) < .0001 && m.Weight(0, 0, color) == 0);
            Check(MathF.Abs((m with { Inverted = true }).Weight(.5f, .5f, color) + normal - .4f) < .0001);
            Check((m with { Enabled = false }).Weight(.5f, .5f, color) == 0);
        });
        test("Moving sample pins retains pixels but changes catalog equality", () =>
        {
            var a = new PhotoState { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red }] };
            var b = a with { Masks = [a.Masks[0] with { ColorRange = Red with { Samples = [Red.Samples[0] with { X = .2f }] } }] };
            Check(PhotoStateEquality.Pixels(a, b) && !PhotoStateEquality.All(a, b));
        });
        test("Color range selects source red without affecting other stripes", () =>
        {
            var p = Stripes(); p.State = new() { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red, Exposure = 1 }] };
            using var r = new PhotoRenderer(); using var image = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            Check(image.GetPixel(50, 90).Red > 230 && Math.Abs(image.GetPixel(150, 90).Green - 170) <= 2 && Math.Abs(image.GetPixel(250, 90).Blue - 180) <= 2);
        });
        test("Color range CPU reference agrees with shader coverage", () =>
        {
            var p = Tiny(); var m = new LocalMask { Kind = MaskKind.ColorRange, ColorRange = new() { Enabled = true, Samples = [new(.5f, .4f, .5f)], Tolerance = .08f, Smoothness = .12f }, Exposure = .65f };
            p.State = new() { Masks = [m] }; using var source = SKBitmap.Decode(p.Original);
            using var r = new PhotoRenderer(); using var output = SKBitmap.Decode(r.Export(p, SKEncodedImageFormat.Png));
            for (var y = 5; y < 60; y += 11) for (var x = 5; x < 90; x += 13)
            {
                var c = source.GetPixel(x, y); var weight = m.Weight((x + .5f) / 96, (y + .5f) / 64, new ColorSample(c.Red / 255f, c.Green / 255f, c.Blue / 255f), 1.5f);
                float Expected(byte channel)
                {
                    var v = channel / 255f; var linear = v <= .04045f ? v / 12.92f : MathF.Pow((v + .055f) / 1.055f, 2.4f);
                    linear *= MathF.Pow(2, m.Exposure); var encoded = linear <= .0031308f ? linear * 12.92f : 1.055f * MathF.Pow(linear, 1 / 2.4f) - .055f;
                    return Numeric.Unit(v + (encoded - v) * weight) * 255;
                }
                var actual = output.GetPixel(x, y);
                Check(Math.Abs(Expected(c.Red) - actual.Red) < 3 && Math.Abs(Expected(c.Green) - actual.Green) < 3 && Math.Abs(Expected(c.Blue) - actual.Blue) < 3);
            }
        });
        test("Bounded source sampling ignores development and coverage", () =>
        {
            var p = Stripes(); p.State = new() { Develop = new() { Exposure = 4, Monochrome = true } };
            using var r = new PhotoRenderer(); var sample = r.SampleSource(p, .2f, .5f)!;
            Check(Math.Abs(sample.Red - 180 / 255f) < .001 && r.SourceSamplePixels == 25 && r.Statistics.ImageDecodes == 1);
            _ = r.SampleSource(p, 0, 0); Check(r.SourceSamplePixels == 34 && r.Statistics.ImageDecodes == 1);
        });
        test("Fully transparent source sampling does not add artificial black", () =>
        {
            using var r = new PhotoRenderer(); Check(r.SampleSource(Solid(SKColors.Transparent), .5f, .5f) is null);
        });
        test("Color-range edits preserve shader decoding and curve tables", () =>
        {
            using var r = new PhotoRenderer(); var p = Stripes(); p.State = new() { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red }] };
            using var s = SKSurface.Create(new SKImageInfo(300, 180)); r.Draw(s.Canvas, p, SKRect.Create(300, 180)); var before = r.Statistics; var curves = r.CurveLookupBuilds;
            p.State = p.State with { Masks = [p.State.Masks[0] with { ColorRange = Red with { Tolerance = .2f } }] }; r.Draw(s.Canvas, p, SKRect.Create(300, 180));
            Check(r.Statistics.ImageDecodes == before.ImageDecodes && r.CurveLookupBuilds == curves && r.Statistics.ShaderBuilds == before.ShaderBuilds + 1);
        });
        test("Color samples roundtrip through the current schema and undo", () =>
        {
            var p = Stripes(); var s = new EditorSession(new CatalogDocument { Photos = [p], ActivePhoto = p.Id });
            s.Edit("Color selection", state => state with { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red }] });
            var loaded = CatalogSerializer.Deserialize(CatalogSerializer.Serialize(s.Catalog));
            Check(loaded.SchemaVersion == CatalogDocument.CurrentSchemaVersion && PhotoStateEquality.All(p.State, loaded.Photos[0].State));
            s.Undo(); Check(p.State.Masks.Length == 0); s.Redo(); Check(p.State.Masks[0].ColorRange.Samples.Length == 1);
        });
        test("Native XMP preserves color ranges and imports legacy schema three", () =>
        {
            var state = new PhotoState { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red }] };
            Check(PhotoStateEquality.All(state, XmpSidecar.Import(XmpSidecar.Export(state).Xml).State));
            var legacy = XmpSidecar.Export(new()).Xml.Replace($"ls:SchemaVersion=\"{XmpSidecar.NativeSchemaVersion}\"", "ls:SchemaVersion=\"3\"");
            var loaded = XmpSidecar.Import(legacy); Check(loaded.UsedNativeSettings && loaded.State.Masks.Length == 0);
        });
        test("Color coverage is viewport-only and does not leak into export", () =>
        {
            var p = Stripes(); p.State = new() { Masks = [new LocalMask { Kind = MaskKind.ColorRange, ColorRange = Red }] };
            using var r = new PhotoRenderer(); var original = r.Export(p, SKEncodedImageFormat.Png);
            using var s = SKSurface.Create(new SKImageInfo(300, 180)); r.Draw(s.Canvas, p, SKRect.Create(300, 180), overlayMask: 0);
            Check(r.Export(p, SKEncodedImageFormat.Png).SequenceEqual(original));
        });
        Directory.CreateDirectory("artifacts/engine"); File.WriteAllBytes("artifacts/engine/color-ranges.png", Stripes().Original);
    }
}
