using System.Runtime.InteropServices;
using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

/// <summary>Small owner-thread cache of immutable floating-point transfer tables, independent of exposure uniforms.</summary>
public sealed class ToneLookupCache : IDisposable
{
    public const int Resolution = 2048;
    private sealed record Entry(ChannelCurves Curves, SKImage Image, long Used);
    private readonly List<Entry> _entries = [];
    private long _clock;
    public long Builds { get; private set; }
    public SKImage Get(ChannelCurves input)
    {
        var curves = input.Normalize();
        for (var i = 0; i < _entries.Count; i++)
        {
            var cached = _entries[i]; if (!cached.Curves.ValueEquals(curves)) continue;
            _entries[i] = cached with { Used = ++_clock }; return cached.Image;
        }
        var master = curves.Master.Compile(); var red = curves.Red.Compile();
        var green = curves.Green.Compile(); var blue = curves.Blue.Compile();
        var pixels = new float[Resolution * 4];
        for (var x = 0; x < Resolution; x++)
        {
            var value = master.Evaluate(x / (float)(Resolution - 1)); var index = x * 4;
            pixels[index] = red.Evaluate(value); pixels[index + 1] = green.Evaluate(value);
            pixels[index + 2] = blue.Evaluate(value); pixels[index + 3] = 1;
        }
        using var bitmap = new SKBitmap(new SKImageInfo(Resolution, 1, SKColorType.RgbaF32, SKAlphaType.Opaque));
        if (bitmap.GetPixels() == IntPtr.Zero) throw new InvalidOperationException("Unable to allocate curve lookup.");
        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        var image = SKImage.FromBitmap(bitmap) ?? throw new InvalidOperationException("Unable to create curve lookup.");
        if (_entries.Count >= 8) { var old = _entries.MinBy(e => e.Used)!; old.Image.Dispose(); _entries.Remove(old); }
        _entries.Add(new(curves, image, ++_clock)); Builds++; return image;
    }
    public void Clear() { foreach (var entry in _entries) entry.Image.Dispose(); _entries.Clear(); }
    public void Dispose() => Clear();
}
