using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class SharedSourceLifetimeTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Shared-source recorded draws survive final owner disposal", () =>
        {
            var pictures = new List<SKPicture>();
            try
            {
                using (var renderer = new PhotoRenderer(maxImages: 1))
                {
                    var active = Tiny(); var reference = ReferencePhotoSnapshot.Capture(active);
                    active.State = new() { Develop = new() { Exposure = 1 } };
                    foreach (var photo in new[] { reference.Photo, active })
                    {
                        using var recorder = new SKPictureRecorder();
                        renderer.Draw(recorder.BeginRecording(SKRect.Create(96, 64)), photo, SKRect.Create(96, 64));
                        pictures.Add(recorder.EndRecording());
                    }
                    Check(renderer.Statistics.ImageDecodes == 1);
                    renderer.Clear(); Check(renderer.Statistics.CachedSources == 0);
                }
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                using var surface = SKSurface.Create(new SKImageInfo(96, 64));
                var colors = new List<SKColor>();
                foreach (var picture in pictures)
                {
                    surface.Canvas.Clear(SKColors.Transparent); surface.Canvas.DrawPicture(picture);
                    using var snapshot = surface.Snapshot(); using var pixels = SKBitmap.FromImage(snapshot);
                    colors.Add(pixels.GetPixel(20, 20));
                }
                Check(colors[0].Alpha == 255 && colors[1].Red > colors[0].Red + 15);
            }
            finally { foreach (var picture in pictures) picture.Dispose(); }
        });
        test("Renderer statistics retain six-field construction and deconstruction", () =>
        {
            var stats = new RendererStatistics(1, 2, 3, 4, 5, 6);
            var (decodes, shaders, draws, bytes, images, samples) = stats;
            Check(decodes == 1 && shaders == 2 && draws == 3 && bytes == 4 && images == 5 && samples == 6);
            Check(stats.CachedSources == 0 && stats.SharedSourceHits == 0);
        });
    }
}
