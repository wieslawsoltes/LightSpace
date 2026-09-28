using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

/// <summary>Owner-thread renderer with bounded source, curve-table and incremental brush caches.</summary>
public sealed partial class PhotoRenderer : IDisposable
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
    private readonly ToneLookupCache _curves = new();
    private readonly BrushCoverageCache _brushes = new();
    private readonly int _previewMaxDimension, _maxImages;
    private readonly long _maxBytes;
    private long _clock, _bytes, _decodes, _shaders, _draws, _autoSamples;
    private bool _disposed;
    public int CachedImages => _cache.Count;
    public string Pipeline => "Skia runtime shader · sRGB";
    public RendererStatistics Statistics => new(_decodes, _shaders, _draws, _bytes, _cache.Count, _autoSamples);
    public long CurveLookupBuilds => _curves.Builds;
    public BrushCacheStatistics BrushStatistics => _brushes.Statistics;

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
            else { using var paint = new SKPaint { Shader = shader, IsAntialias = false }; canvas.DrawRect(0, 0, image.Width, image.Height, paint); }
        }
        finally { canvas.Restore(); }
    }
    public SKShader CreateShader(SKImage image, PhotoState state, int sourceWidth = 0, int sourceHeight = 0, int overlayMask = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(image);
        state = state.Normalize();
        var scale = Math.Min(1f, 1024f / Math.Max(image.Width, image.Height));
        var width = Math.Max(1, (int)MathF.Round(image.Width * scale));
        var height = Math.Max(1, (int)MathF.Round(image.Height * scale));
        var scope = new RuntimeShaderScope(_effect);
        try
        {
            ConfigureUniforms(scope.Uniforms, image, state, sourceWidth, sourceHeight, width, height, overlayMask);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear);
            scope.Bind("original", image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling));
            scope.Bind("toneLookup", _curves.Get(state.Develop.Channels).ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling));
            for (var i = 0; i < 8; i++)
            {
                var texture = i < state.Masks.Length ? _brushes.Get(state.Masks[i], width, height) : _brushes.Identity;
                scope.Bind("brush" + i, texture.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling));
            }
            scope.Compile();
            scope.ReleaseInputs();
        }
        catch { scope.Dispose(); throw; }
        GC.KeepAlive(image);
        _shaders++;
        return scope.TakeResult();
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
    public void Clear()
    {
        foreach (var p in _cache.Values) p.Dispose(); _cache.Clear(); _bytes = 0;
        _curves.Clear(); _brushes.Clear();
    }
    public void Dispose() { if (_disposed) return; Clear(); _effect.Dispose(); _curves.Dispose(); _brushes.Dispose(); _disposed = true; }
}
