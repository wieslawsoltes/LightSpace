using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public sealed partial class PhotoRenderer
{
    public long SourceSamplePixels { get; private set; }
    private static void SetColorUniforms(SKRuntimeEffectUniforms uniforms, LocalMask[] masks)
    {
        var controls = new float[32]; var samples = Enumerable.Range(0, 5).Select(_ => new float[32]).ToArray(); var enabled = false;
        for (var i = 0; i < masks.Length; i++)
        {
            var settings = masks[i].ColorRange;
            var active = settings.Enabled || masks[i].Kind == MaskKind.ColorRange;
            if (active && masks[i].Enabled && masks[i].Opacity > 0) enabled = true;
            controls[i * 4] = settings.Tolerance; controls[i * 4 + 1] = settings.Smoothness;
            controls[i * 4 + 2] = settings.Samples.Length; controls[i * 4 + 3] = active ? 1 : 0;
            for (var j = 0; j < settings.Samples.Length; j++)
            {
                var color = settings.Samples[j].ToOklab();
                samples[j][i * 4] = color.X; samples[j][i * 4 + 1] = color.Y;
                samples[j][i * 4 + 2] = color.Z; samples[j][i * 4 + 3] = 1;
            }
        }
        uniforms["useColorRanges"] = enabled ? 1 : 0; uniforms["maskColors"] = controls;
        for (var i = 0; i < 5; i++) uniforms["maskSample" + i] = samples[i];
    }

    /// <summary>Read a bounded patch of the retained source preview; never copy the whole image or sample developed/overlay pixels.</summary>
    public ColorSample? SampleSource(PhotoDocument photo, float u, float v, int radius = 2)
    {
        if (radius is < 0 or > 8) throw new ArgumentOutOfRangeException(nameof(radius));
        var image = GetImage(photo).Image; u = Numeric.Unit(u); v = Numeric.Unit(v);
        var x = Math.Min(image.Width - 1, (int)(u * image.Width)); var y = Math.Min(image.Height - 1, (int)(v * image.Height));
        var left = Math.Max(0, x - radius); var top = Math.Max(0, y - radius);
        var width = Math.Min(image.Width, x + radius + 1) - left; var height = Math.Min(image.Height, y + radius + 1) - top;
        using var srgb = SKColorSpace.CreateSrgb();
        using var patch = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul, srgb));
        if (!image.ReadPixels(patch.Info, patch.GetPixels(), patch.RowBytes, left, top)) throw new InvalidOperationException("Source color sampling failed.");
        SourceSamplePixels += (long)width * height; double r = 0, g = 0, b = 0, alpha = 0;
        foreach (var pixel in patch.Pixels)
        {
            var weight = pixel.Alpha / 255d; r += pixel.Red / 255d * weight; g += pixel.Green / 255d * weight; b += pixel.Blue / 255d * weight; alpha += weight;
        }
        return alpha < .00001 ? null : new((float)(r / alpha), (float)(g / alpha), (float)(b / alpha), u, v);
    }
}
