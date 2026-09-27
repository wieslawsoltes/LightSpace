using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

/// <summary>Owner-thread-confined renderer with byte-budgeted decode caching and pixel-aware shader reuse.</summary>
public sealed class PhotoRenderer : IDisposable
{
    private sealed class CachedPhoto(SKImage image, byte[] original) : IDisposable
    {
        public SKImage Image { get; } = image;
        public byte[] Original { get; } = original;
        public PhotoState? State { get; set; }
        public int Overlay { get; set; } = -1;
        public SKShader? Shader { get; set; }
        public long Used { get; set; }
        public long Bytes => (long)Image.Width * Image.Height * 4;
        public void Dispose() { Shader?.Dispose(); Image.Dispose(); }
    }
    private readonly Dictionary<Guid, CachedPhoto> _cache = [];
    private readonly SKRuntimeEffect _effect;
    private readonly int _previewMaxDimension, _maxImages;
    private readonly long _maxBytes;
    private long _clock, _bytes, _decodes, _shaders, _draws, _autoSamples;
    private bool _disposed;
    public int CachedImages => _cache.Count;
    public string Pipeline => "Skia runtime shader · sRGB";
    public RendererStatistics Statistics => new(_decodes, _shaders, _draws, _bytes, _cache.Count, _autoSamples);

    public PhotoRenderer(int previewMaxDimension = 2560, int maxImages = 5, long maxDecodedBytes = 128L * 1024 * 1024)
    {
        if (previewMaxDimension is < 64 or > 4096) throw new ArgumentOutOfRangeException(nameof(previewMaxDimension));
        if (maxImages < 1) throw new ArgumentOutOfRangeException(nameof(maxImages));
        if (maxDecodedBytes < (long)previewMaxDimension * previewMaxDimension * 4) throw new ArgumentOutOfRangeException(nameof(maxDecodedBytes), "Budget must accommodate one maximum-size preview.");
        _previewMaxDimension = previewMaxDimension; _maxImages = maxImages; _maxBytes = maxDecodedBytes;
        using var stream = typeof(PhotoRenderer).Assembly.GetManifestResourceStream("LightSpace.Rendering.Skia.Shaders.Develop.sksl") ?? throw new InvalidOperationException("Missing development shader.");
        using var reader = new StreamReader(stream);
        _effect = SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out var errors) ?? throw new InvalidOperationException($"Development shader: {errors}");
    }
    private void Remove(Guid id)
    {
        if (!_cache.Remove(id, out var entry)) return;
        _bytes -= entry.Bytes; entry.Dispose();
    }
    private CachedPhoto GetImage(PhotoDocument photo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_cache.TryGetValue(photo.Id, out var value) && !ReferenceEquals(value.Original, photo.Original)) { Remove(photo.Id); value = null; }
        if (value is null)
        {
            var image = PhotoCodec.Decode(photo.Original, _previewMaxDimension); _decodes++;
            value = new(image, photo.Original);
            while (_cache.Count > 0 && (_cache.Count >= _maxImages || _bytes + value.Bytes > _maxBytes)) Remove(_cache.MinBy(p => p.Value.Used).Key);
            _cache.Add(photo.Id, value); _bytes += value.Bytes;
        }
        value.Used = ++_clock; return value;
    }
    private SKShader? GetShader(CachedPhoto cache, PhotoDocument photo, int overlayMask)
    {
        if (cache.State is null || cache.Overlay != overlayMask || !PhotoStateEquality.Shader(cache.State, photo.State))
        {
            var next = overlayMask < 0 && IsNeutral(photo.State) ? null : CreateShader(cache.Image, photo.State, photo.Width, photo.Height, overlayMask);
            cache.Shader?.Dispose(); cache.Shader = next; cache.Overlay = overlayMask;
        }
        // Keep the newest metadata snapshot without rebuilding an identical shader.
        cache.State = photo.State; return cache.Shader;
    }
    private static bool IsNeutral(PhotoState state) => state.Masks.Length == 0 && state.CloneSpots.Length == 0
        && PhotoStateEquality.Develop(state.Develop, DevelopSettings.Default);

    public void Draw(SKCanvas canvas, PhotoDocument photo, SKRect destination, bool original = false, bool uncropped = false, int overlayMask = -1)
    {
        var cache = GetImage(photo); var shader = original ? null : GetShader(cache, photo, overlayMask);
        DrawSource(canvas, cache.Image, shader, uncropped ? new() : photo.State.Crop, destination); _draws++;
    }
    private static void DrawSource(SKCanvas canvas, SKImage image, SKShader? shader, CropSettings crop, SKRect destination)
    {
        canvas.Save();
        try
        {
            canvas.ClipRect(destination); var matrix = PhotoTransform.SourceToView(crop, image.Width, image.Height, destination); canvas.Concat(in matrix);
            if (shader is null) canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
            else { using var paint = new SKPaint { Shader = shader }; canvas.DrawRect(0, 0, image.Width, image.Height, paint); }
        }
        finally { canvas.Restore(); }
    }
    public SKShader CreateShader(SKImage image, PhotoState state, int sourceWidth = 0, int sourceHeight = 0, int overlayMask = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        state = state.Normalize(); var s = state.Develop;
        var u = new SKRuntimeEffectUniforms(_effect)
        {
            ["size"] = new float[] { image.Width, image.Height },
            ["sourceSize"] = new float[] { sourceWidth > 0 ? sourceWidth : image.Width, sourceHeight > 0 ? sourceHeight : image.Height },
            ["light"] = new[] { s.Exposure, s.Contrast / 100, s.Highlights / 100, s.Shadows / 100 },
            ["tone"] = new[] { s.Whites / 100, s.Blacks / 100, s.Temperature / 100, s.Tint / 100 },
            ["color"] = new[] { s.Vibrance / 100, s.Saturation / 100, s.Monochrome ? 1f : 0, s.Dehaze / 100 },
            ["detail"] = new[] { s.Texture / 100, s.Clarity / 100, s.Sharpening / 100, s.NoiseReduction / 100 },
            ["effects"] = new[] { s.Vignette / 100, s.Grain / 100 },
            ["curve"] = new[] { s.Curve.Black, s.Curve.Shadow, s.Curve.Mid, s.Curve.Light }, ["curveWhite"] = s.Curve.White,
            ["useCurve"] = s.Curve.IsIdentity ? 0 : 1,
            ["useMixer"] = s.Mixer.Any(b => b.Hue != 0 || b.Saturation != 0 || b.Luminance != 0) ? 1 : 0,
            ["mixer"] = s.Mixer.SelectMany(b => new[] { b.Hue / 100, b.Saturation / 100, b.Luminance / 100, 0 }).ToArray(),
            ["useGrading"] = s.Grading.IsNeutral ? 0 : 1,
            ["gradeMix"] = new[] { s.Grading.Blending / 100, s.Grading.Balance / 100 },
            ["overlayMask"] = overlayMask
        };
        var grading = new float[16];
        for (var i = 0; i < 4; i++)
        {
            var grade = s.Grading.Get((GradingRange)i); var v = grade.TintVector();
            grading[i * 4] = v.X; grading[i * 4 + 1] = v.Y; grading[i * 4 + 2] = v.Z; grading[i * 4 + 3] = grade.Luminance / 100;
        }
        u["grading"] = grading;
        var geometry = new float[32]; var adjustments = new float[32]; var controls = new float[32]; var ranges = new float[32]; var extra = new float[32];
        for (var i = 0; i < state.Masks.Length; i++)
        {
            var m = state.Masks[i]; var a = m.Angle * MathF.PI / 180;
            new[] { m.X, m.Y, m.RadiusX, m.RadiusY }.CopyTo(geometry, i * 4);
            new[] { m.Exposure, m.Saturation / 100, m.Feather, m.Inverted ? 1f : 0 }.CopyTo(adjustments, i * 4);
            new[] { MathF.Cos(a), MathF.Sin(a), (float)m.Kind, m.Enabled ? m.Opacity : 0 }.CopyTo(controls, i * 4);
            new[] { m.RangeMin, m.RangeMax, m.RangeSmoothness, m.RangeEnabled || m.Kind == MaskKind.LuminanceRange ? 1f : 0 }.CopyTo(ranges, i * 4);
            new[] { m.Contrast / 100, m.Temperature / 100, m.Tint / 100, 0 }.CopyTo(extra, i * 4);
        }
        u["maskCount"] = state.Masks.Length; u["maskGeometry"] = geometry; u["maskAdjust"] = adjustments;
        u["maskControl"] = controls; u["maskRange"] = ranges; u["maskExtra"] = extra;
        var spotData = new float[128]; var radii = new float[32];
        for (var i = 0; i < state.CloneSpots.Length; i++) { var p = state.CloneSpots[i]; new[] { p.X, p.Y, p.SourceX, p.SourceY }.CopyTo(spotData, i * 4); radii[i] = p.Radius; }
        u["spotCount"] = state.CloneSpots.Length; u["spots"] = spotData; u["radii"] = radii;
        using var child = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear));
        var result = _effect.ToShader(u, new SKRuntimeEffectChildren(_effect) { ["original"] = child }); _shaders++; return result;
    }
    public byte[] Export(PhotoDocument photo, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg, int quality = 92, int maxDimension = 0)
    {
        using var image = PhotoCodec.Decode(photo.Original, 0); using var shader = IsNeutral(photo.State) ? null : CreateShader(image, photo.State, photo.Width, photo.Height);
        var (w, h) = photo.State.Crop.OutputSize(image.Width, image.Height);
        var limit = maxDimension <= 0 ? 8192 : Math.Clamp(maxDimension, 64, 8192); var scale = Math.Min(1f, (float)limit / Math.Max(w, h));
        w = Math.Max(1, (int)Math.Round(w * scale)); h = Math.Max(1, (int)Math.Round(h * scale));
        using var srgb = SKColorSpace.CreateSrgb(); using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul, srgb)) ?? throw new InvalidOperationException("Unable to allocate export surface.");
        surface.Canvas.Clear(format == SKEncodedImageFormat.Jpeg ? SKColors.White : SKColors.Transparent);
        DrawSource(surface.Canvas, image, shader, photo.State.Crop, SKRect.Create(w, h));
        using var output = surface.Snapshot(); using var data = output.Encode(format, Math.Clamp(quality, 1, 100)) ?? throw new InvalidOperationException("Image encoding failed."); return data.ToArray();
    }
    public Histogram CalculateHistogram(PhotoDocument photo)
    {
        var cache = GetImage(photo); using var surface = SKSurface.Create(new SKImageInfo(192, 128, SKColorType.Rgba8888, SKAlphaType.Premul));
        // Histograms must not change the viewport shader's overlay cache key.
        using var temporary = cache.Overlay >= 0 && !IsNeutral(photo.State) ? CreateShader(cache.Image, photo.State, photo.Width, photo.Height) : null;
        var shader = cache.Overlay >= 0 ? temporary : GetShader(cache, photo, -1);
        surface.Canvas.Clear(SKColors.Transparent); DrawSource(surface.Canvas, cache.Image, shader, photo.State.Crop, SKRect.Create(192, 128));
        using var snapshot = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(snapshot); return Histogram.FromPixels(bitmap.Pixels);
    }
    public DevelopSettings Auto(PhotoDocument photo)
    {
        var cache = GetImage(photo);
        using var surface = SKSurface.Create(new SKImageInfo(96, 64)); surface.Canvas.DrawImage(cache.Image, SKRect.Create(96, 64), new SKSamplingOptions(SKFilterMode.Linear));
        using var image = surface.Snapshot(); using var bitmap = SKBitmap.FromImage(image); var pixels = bitmap.Pixels;
        double sum = 0; var count = 0;
        foreach (var p in pixels) { if (p.Alpha == 0) continue; sum += (.2126 * p.Red + .7152 * p.Green + .0722 * p.Blue) / 255; count++; }
        _autoSamples += pixels.Length;
        var average = count == 0 ? .5 : sum / count;
        return photo.State.Develop with { Exposure = (float)Math.Clamp(Math.Log2(.47 / Math.Max(.02, average)), -2, 2), Highlights = -22, Shadows = 20, Contrast = 8, Vibrance = 12 };
    }
    public void Clear() { foreach (var p in _cache.Values) p.Dispose(); _cache.Clear(); _bytes = 0; }
    public void Dispose() { if (_disposed) return; Clear(); _effect.Dispose(); _disposed = true; }
}
