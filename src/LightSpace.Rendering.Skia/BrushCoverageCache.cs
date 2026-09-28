using System.Runtime.InteropServices;
using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public sealed record BrushCacheStatistics(long Replays, long DabsRasterized, long PixelsVisited, long TextureBuilds, long RetainedBytes);

/// <summary>
/// Incremental affine mask composition: coverage = multiplier * analyticCoverage + bias.
/// Only newly appended dabs are rasterized; changing local exposure never replays strokes.
/// Float accumulation avoids quantization drift. The immutable RG texture is sampled by Skia.
/// </summary>
public sealed class BrushCoverageCache : IDisposable
{
    private sealed class Entry(int width, int height)
    {
        public int Width { get; } = width;
        public int Height { get; } = height;
        public float[] Multiplier { get; } = new float[width * height];
        public float[] Bias { get; } = new float[width * height];
        public float[] Stroke { get; } = new float[width * height];
        public BrushStroke[] Previous { get; set; } = [];
        public SKImage? Image { get; set; }
        public long Used { get; set; }
        public long Bytes => (long)Width * Height * 16;
        public void Reset() { Array.Fill(Multiplier, 1); Array.Clear(Bias); Array.Clear(Stroke); Previous = []; }
    }
    private readonly Dictionary<(Guid Id, int Width, int Height), Entry> _entries = [];
    private readonly long _budget;
    private readonly SKImage _identity;
    private long _clock, _bytes, _replays, _dabs, _pixels, _textures;
    public BrushCacheStatistics Statistics => new(_replays, _dabs, _pixels, _textures, _bytes);
    public BrushCoverageCache(long budget = 128L * 1024 * 1024)
    {
        if (budget < 16L * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(budget));
        _budget = budget;
        using var bitmap = new SKBitmap(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Opaque));
        bitmap.Erase(new SKColor(255, 0, 0, 255)); _identity = SKImage.FromBitmap(bitmap);
    }
    public SKImage Identity => _identity;
    public SKImage Get(LocalMask mask, int width, int height)
    {
        if (width is < 1 or > 2048 || height is < 1 or > 2048) throw new ArgumentOutOfRangeException(nameof(width));
        var strokes = BrushStroke.NormalizeAll(mask.Strokes);
        if (strokes.Length == 0) return _identity;
        var key = (mask.Id, width, height);
        if (!_entries.TryGetValue(key, out var entry))
        {
            var bytes = (long)width * height * 16;
            if (bytes > _budget) throw new InvalidOperationException("Brush surface exceeds its cache budget.");
            while (_entries.Count > 0 && _bytes + bytes > _budget)
            {
                var oldest = _entries.MinBy(e => e.Value.Used); oldest.Value.Image?.Dispose();
                _bytes -= oldest.Value.Bytes; _entries.Remove(oldest.Key);
            }
            entry = new(width, height); entry.Reset(); _entries.Add(key, entry); _bytes += bytes;
        }
        entry.Used = ++_clock;
        if (entry.Image is not null && BrushStroke.SequenceEquals(entry.Previous, strokes)) return entry.Image;
        var previous = entry.Previous;
        var append = CanAppend(previous, strokes);
        var fromStroke = append && previous.Length > 0 ? previous.Length - 1 : 0;
        var fromDab = append && previous.Length > 0 ? previous[^1].Dabs.Length : 0;
        if (!append) { entry.Reset(); _replays++; }
        var visitedAtStart = _pixels;
        try
        {
            for (var s = fromStroke; s < strokes.Length; s++)
            {
                if (s != fromStroke || !append || previous.Length == 0) Array.Clear(entry.Stroke);
                var stroke = strokes[s];
                for (var d = s == fromStroke ? fromDab : 0; d < stroke.Dabs.Length; d++)
                {
                    Stamp(entry, stroke, stroke.Dabs[d]);
                    if (_pixels - visitedAtStart > 200_000_000)
                        throw new InvalidDataException("Brush replay exceeds the processing safety budget. Split this mask into smaller edits.");
                }
            }
            // Publishing is atomic: cached state is advanced only after all dabs succeed.
            var next = Snapshot(entry); entry.Image?.Dispose(); entry.Image = next;
            entry.Previous = strokes; _textures++; return next;
        }
        catch
        {
            entry.Reset(); entry.Image?.Dispose(); entry.Image = null; throw;
        }
    }
    private static bool CanAppend(BrushStroke[] previous, BrushStroke[] next)
    {
        if (previous.Length == 0) return true;
        if (next.Length < previous.Length) return false;
        for (var i = 0; i < previous.Length - 1; i++) if (!previous[i].PixelEquals(next[i])) return false;
        var a = previous[^1]; var b = next[previous.Length - 1];
        return a.SameParameters(b) && b.Dabs.Length >= a.Dabs.Length && a.Dabs.AsSpan().SequenceEqual(b.Dabs.AsSpan(0, a.Dabs.Length));
    }
    private void Stamp(Entry entry, BrushStroke stroke, BrushDab dab)
    {
        var width = entry.Width; var height = entry.Height;
        var radius = stroke.Radius * (.2f + .8f * dab.Pressure) * height;
        var cx = dab.X * width; var cy = dab.Y * height;
        var left = Math.Clamp((int)MathF.Floor(cx - radius), 0, width);
        var right = Math.Clamp((int)MathF.Ceiling(cx + radius), 0, width);
        var top = Math.Clamp((int)MathF.Floor(cy - radius), 0, height);
        var bottom = Math.Clamp((int)MathF.Ceiling(cy + radius), 0, height);
        var flow = stroke.Flow * dab.Pressure;
        for (var y = top; y < bottom; y++) for (var x = left; x < right; x++)
        {
            _pixels++; var dx = x + .5f - cx; var dy = y + .5f - cy;
            var distance = MathF.Sqrt(dx * dx + dy * dy) / MathF.Max(.0001f, radius);
            if (distance > 1) continue;
            var weight = stroke.Feather <= 0 ? 1 : 1 - Numeric.Smooth(1 - stroke.Feather, 1, distance);
            var i = y * width + x; var old = entry.Stroke[i];
            var coverage = old + (stroke.Density - old) * flow * weight;
            // Incremental equivalent of compositing the complete current stroke once.
            var delta = old >= 1 ? 0 : Numeric.Unit((coverage - old) / (1 - old));
            entry.Stroke[i] = coverage;
            entry.Multiplier[i] *= 1 - delta;
            entry.Bias[i] = stroke.Erase ? entry.Bias[i] * (1 - delta) : delta + (1 - delta) * entry.Bias[i];
        }
        _dabs++;
    }
    private static SKImage Snapshot(Entry entry)
    {
        var pixels = new byte[checked(entry.Width * entry.Height * 4)];
        for (var i = 0; i < entry.Multiplier.Length; i++)
        {
            pixels[i * 4] = (byte)Math.Clamp((int)MathF.Round(entry.Multiplier[i] * 255), 0, 255);
            pixels[i * 4 + 1] = (byte)Math.Clamp((int)MathF.Round(entry.Bias[i] * 255), 0, 255);
            pixels[i * 4 + 3] = 255;
        }
        using var bitmap = new SKBitmap(new SKImageInfo(entry.Width, entry.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        if (bitmap.GetPixels() == IntPtr.Zero) throw new InvalidOperationException("Unable to allocate brush texture.");
        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        return SKImage.FromBitmap(bitmap) ?? throw new InvalidOperationException("Unable to create brush texture.");
    }
    public void Clear()
    {
        foreach (var entry in _entries.Values) entry.Image?.Dispose();
        _entries.Clear(); _bytes = 0;
    }
    public void Dispose() { Clear(); _identity.Dispose(); }
}
