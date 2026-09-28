using LightSpace.Core;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class RendererLifetimeTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Shader replacements and neutral transitions survive forced finalization", ReplacementAndNeutral);
        test("Recorded photo draws retain resources after renderer disposal", RecordedDraws);
        test("Shader uniform snapshots stay immutable after later shader creation", IndependentUniformSnapshots);
    }

    private static void Collect()
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    }

    private static void ReplacementAndNeutral()
    {
        using var renderer = new PhotoRenderer();
        using var surface = SKSurface.Create(new SKImageInfo(96, 64));
        var photo = Tiny();
        float[] sequence = [4, -4, 3, -3, 4.9f, -4.9f, 1, -1, 0, 2.5f, 0];
        for (var iteration = 0; iteration < 24; iteration++)
        {
            foreach (var exposure in sequence)
            {
                photo.State = photo.State with { Develop = photo.State.Develop with { Exposure = exposure } };
                renderer.Draw(surface.Canvas, photo, SKRect.Create(96, 64));
                if (exposure == 0) Collect();
                var histogram = renderer.CalculateHistogram(photo);
                Check(histogram.Count == 192 * 128);
            }
        }
        using var snapshot = surface.Snapshot(); using var actual = SKBitmap.FromImage(snapshot);
        using var expected = SKBitmap.Decode(photo.Original);
        var a = actual.GetPixel(30, 30); var b = expected.GetPixel(30, 30);
        Check(Math.Abs(a.Red - b.Red) <= 2 && Math.Abs(a.Green - b.Green) <= 2 && Math.Abs(a.Blue - b.Blue) <= 2);
        Check(renderer.Statistics.ImageDecodes == 1);
    }

    private static void RecordedDraws()
    {
        var recordings = new List<SKPicture>();
        try
        {
            using (var renderer = new PhotoRenderer(maxImages: 1))
            {
                var photo = Tiny();
                for (var i = 0; i < 24; i++)
                {
                    photo.State = photo.State with { Develop = photo.State.Develop with { Exposure = (i % 7 - 3) * .3f } };
                    using var recorder = new SKPictureRecorder();
                    var canvas = recorder.BeginRecording(SKRect.Create(96, 64));
                    renderer.Draw(canvas, photo, SKRect.Create(96, 64));
                    recordings.Add(recorder.EndRecording());
                    if (i % 3 == 0) renderer.Clear();
                }
            }
            Collect();
            using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            foreach (var recording in recordings)
            {
                surface.Canvas.Clear(SKColors.Transparent); surface.Canvas.DrawPicture(recording);
                using var image = surface.Snapshot(); using var pixels = SKBitmap.FromImage(image);
                Check(pixels.GetPixel(30, 30).Alpha == 255);
            }
        }
        finally { foreach (var recording in recordings) recording.Dispose(); }
    }

    private static void IndependentUniformSnapshots()
    {
        var photo = Tiny(); using var image = PhotoCodec.Decode(photo.Original);
        using var renderer = new PhotoRenderer();
        using var first = renderer.CreateShader(image, new PhotoState { Develop = new() { Exposure = 1 } });
        using var second = renderer.CreateShader(image, new PhotoState { Develop = new() { Exposure = -1 } });
        renderer.Clear(); Collect();
        using var surface = SKSurface.Create(new SKImageInfo(96, 64));
        SKColor Pixel(SKShader shader)
        {
            using var paint = new SKPaint { Shader = shader };
            surface.Canvas.DrawRect(SKRect.Create(96, 64), paint);
            using var rendered = surface.Snapshot(); using var pixels = SKBitmap.FromImage(rendered);
            return pixels.GetPixel(30, 30);
        }
        var bright = Pixel(first); var dark = Pixel(second); var again = Pixel(first);
        Check(bright.Red > dark.Red + 30 && bright == again);
    }
}
