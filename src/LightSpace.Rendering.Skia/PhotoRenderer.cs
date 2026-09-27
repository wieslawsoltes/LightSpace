using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public sealed class PhotoRenderer : IDisposable
{
    private sealed class CachedPhoto(SKImage image) : IDisposable
    {
        public SKImage Image { get; } = image;
        public SKShader? Shader { get; set; }
        public long Revision { get; set; } = -1;
        public long Used { get; set; }
        public void Dispose() { Shader?.Dispose(); Image.Dispose(); }
    }
    private readonly Dictionary<Guid, CachedPhoto> _cache = [];
    private readonly SKRuntimeEffect _effect;
    private long _clock;
    public int CachedImages => _cache.Count;
    public string Pipeline => "Skia runtime shader · sRGB";
    public PhotoRenderer()
    {
        using var stream = typeof(PhotoRenderer).Assembly.GetManifestResourceStream("LightSpace.Rendering.Skia.Shaders.Develop.sksl") ?? throw new InvalidOperationException("Missing development shader.");
        using var reader = new StreamReader(stream);
        _effect = SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out var errors) ?? throw new InvalidOperationException($"Development shader: {errors}");
    }
    private CachedPhoto Get(PhotoDocument photo)
    {
        if (!_cache.TryGetValue(photo.Id, out var value))
        {
            while (_cache.Count >= 5) { var oldest = _cache.MinBy(p => p.Value.Used); oldest.Value.Dispose(); _cache.Remove(oldest.Key); }
            value = new(PhotoCodec.Decode(photo.Original)); _cache.Add(photo.Id, value);
        }
        value.Used = ++_clock;
        if (value.Shader is null || value.Revision != photo.Revision)
        {
            value.Shader?.Dispose(); value.Shader = CreateShader(value.Image, photo.State); value.Revision = photo.Revision;
        }
        return value;
    }
    public void Draw(SKCanvas canvas, PhotoDocument photo, SKRect destination, bool original = false, bool uncropped = false)
    {
        var cache = Get(photo); DrawSource(canvas, cache.Image, cache.Shader!, uncropped ? new() : photo.State.Crop, destination, original);
    }
    private static void DrawSource(SKCanvas canvas, SKImage image, SKShader shader, CropSettings crop, SKRect destination, bool original)
    {
        canvas.Save(); canvas.ClipRect(destination); var matrix = PhotoTransform.SourceToView(crop, image.Width, image.Height, destination); canvas.Concat(ref matrix);
        if (original) canvas.DrawImage(image, 0, 0);
        else { using var paint = new SKPaint { Shader = shader, IsAntialias = false }; canvas.DrawRect(0, 0, image.Width, image.Height, paint); }
        canvas.Restore();
    }
    public SKShader CreateShader(SKImage image, PhotoState state)
    {
        var s = state.Develop.Normalize(); var u = new SKRuntimeEffectUniforms(_effect)
        {
            ["size"] = new float[] { image.Width, image.Height },
            ["light"] = new[] { s.Exposure, s.Contrast / 100, s.Highlights / 100, s.Shadows / 100 },
            ["tone"] = new[] { s.Whites / 100, s.Blacks / 100, s.Temperature / 100, s.Tint / 100 },
            ["color"] = new[] { s.Vibrance / 100, s.Saturation / 100, s.Monochrome ? 1f : 0, s.Dehaze / 100 },
            ["detail"] = new[] { s.Texture / 100, s.Clarity / 100, s.Sharpening / 100, s.NoiseReduction / 100 },
            ["effects"] = new[] { s.Vignette / 100, s.Grain / 100 },
            ["curve"] = new[] { s.Curve.Black, s.Curve.Shadow, s.Curve.Mid, s.Curve.Light }, ["curveWhite"] = s.Curve.White,
            ["mixer"] = s.Mixer.SelectMany(b => new[] { b.Hue / 100, b.Saturation / 100, b.Luminance / 100, 0 }).ToArray()
        };
        var masks = state.Masks.Take(8).ToArray(); var geometry = new float[32]; var adjustments = new float[32]; var kinds = new float[8];
        for (var i = 0; i < masks.Length; i++)
        {
            var m = masks[i].Normalize(); new[] { m.X, m.Y, m.RadiusX, m.RadiusY }.CopyTo(geometry, i * 4);
            new[] { m.Exposure, m.Saturation / 100, m.Feather, m.Inverted ? 1f : 0 }.CopyTo(adjustments, i * 4); kinds[i] = (float)m.Kind;
        }
        u["maskCount"] = masks.Length; u["maskGeometry"] = geometry; u["maskAdjust"] = adjustments; u["maskKind"] = kinds;
        var spots = state.CloneSpots.Take(32).ToArray(); var spotData = new float[128]; var radii = new float[32];
        for (var i = 0; i < spots.Length; i++) { var p = spots[i].Normalize(); new[] { p.X, p.Y, p.SourceX, p.SourceY }.CopyTo(spotData, i * 4); radii[i] = p.Radius; }
        u["spotCount"] = spots.Length; u["spots"] = spotData; u["radii"] = radii;
        using var child = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear));
        return _effect.ToShader(u, new SKRuntimeEffectChildren(_effect) { ["original"] = child });
    }
    public byte[] Export(PhotoDocument photo, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg, int quality = 92, int maxDimension = 0)
    {
        using var image = PhotoCodec.Decode(photo.Original, 0); using var shader = CreateShader(image, photo.State);
        var (w, h) = photo.State.Crop.OutputSize(image.Width, image.Height);
        var limit = maxDimension <= 0 ? 8192 : Math.Clamp(maxDimension, 64, 8192); var scale = Math.Min(1f, (float)limit / Math.Max(w, h));
        w = Math.Max(1, (int)Math.Round(w * scale)); h = Math.Max(1, (int)Math.Round(h * scale));
        using var srgb = SKColorSpace.CreateSrgb(); using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul, srgb)) ?? throw new InvalidOperationException("Unable to allocate export surface.");
        surface.Canvas.Clear(format == SKEncodedImageFormat.Jpeg ? SKColors.White : SKColors.Transparent);
        DrawSource(surface.Canvas, image, shader, photo.State.Crop, SKRect.Create(w, h), false);
        using var output = surface.Snapshot(); using var data = output.Encode(format, Math.Clamp(quality, 1, 100)) ?? throw new InvalidOperationException("Image encoding failed."); return data.ToArray();
    }
    public Histogram CalculateHistogram(PhotoDocument photo)
    {
        var cache = Get(photo); using var surface = SKSurface.Create(new SKImageInfo(192, 128, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent); DrawSource(surface.Canvas, cache.Image, cache.Shader!, photo.State.Crop, SKRect.Create(192, 128), false);
        using var snapshot = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(snapshot); return Histogram.FromPixels(bitmap.Pixels);
    }
    public DevelopSettings Auto(PhotoDocument photo)
    {
        var cache = Get(photo); using var bitmap = SKBitmap.FromImage(cache.Image); var pixels = bitmap.Pixels; double sum = 0; var count = 0;
        for (var i = 0; i < pixels.Length; i += 17) { var p = pixels[i]; if (p.Alpha == 0) continue; sum += (.2126 * p.Red + .7152 * p.Green + .0722 * p.Blue) / 255; count++; }
        var average = count == 0 ? .5 : sum / count;
        return photo.State.Develop with { Exposure = (float)Math.Clamp(Math.Log2(.47 / Math.Max(.02, average)), -2, 2), Highlights = -22, Shadows = 20, Contrast = 8, Vibrance = 12 };
    }
    public void Clear() { foreach (var p in _cache.Values) p.Dispose(); _cache.Clear(); }
    public void Dispose() { Clear(); _effect.Dispose(); }
}
