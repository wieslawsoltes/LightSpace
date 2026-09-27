using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Imaging;

public static class PhotoCodec
{
    public const int MaxEncodedBytes = 64 * 1024 * 1024;
    public const long MaxPixels = 100_000_000;
    public static PhotoDocument Import(string name, byte[] bytes)
    {
        using var codec = Open(bytes);
        var rotated = (int)codec.EncodedOrigin >= 5;
        return new PhotoDocument
        {
            Name = Path.GetFileName(name), Original = bytes,
            Width = rotated ? codec.Info.Height : codec.Info.Width, Height = rotated ? codec.Info.Width : codec.Info.Height
        };
    }
    private static SKCodec Open(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxEncodedBytes) throw new InvalidDataException("Each image must be smaller than 64 MiB.");
        using var data = SKData.CreateCopy(bytes);
        var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Unsupported image. Import JPEG, PNG, WebP, BMP or GIF. Camera RAW decoding is not included.");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > MaxPixels)
        { codec.Dispose(); throw new InvalidDataException("Image exceeds the 100 megapixel decoding limit."); }
        return codec;
    }
    public static SKImage Decode(byte[] bytes, int maxDimension = 2560)
    {
        using var codec = Open(bytes); var original = codec.Info;
        var factor = maxDimension <= 0 ? 1f : Math.Min(1f, (float)maxDimension / Math.Max(original.Width, original.Height));
        var dimensions = codec.GetScaledDimensions(factor);
        using var srgb = SKColorSpace.CreateSrgb();
        var info = new SKImageInfo(dimensions.Width, dimensions.Height, SKColorType.Rgba8888, SKAlphaType.Premul, srgb);
        using var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput) throw new InvalidDataException($"Unable to decode image: {result}.");
        var w = bitmap.Width; var h = bitmap.Height; var o = (int)codec.EncodedOrigin;
        var swap = o >= 5;
        var outputWidth = swap ? h : w; var outputHeight = swap ? w : h;
        var resize = maxDimension > 0 ? Math.Min(1f, (float)maxDimension / Math.Max(outputWidth, outputHeight)) : 1f;
        using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)(outputWidth * resize)), Math.Max(1, (int)(outputHeight * resize)), SKColorType.Rgba8888, SKAlphaType.Premul, srgb))
            ?? throw new InvalidOperationException("Unable to allocate image surface.");
        var c = surface.Canvas; c.Clear(SKColors.Transparent); c.Scale(resize);
        var matrix = o switch
        {
            2 => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            3 => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            4 => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            5 => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            6 => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            7 => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            8 => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity
        };
        c.Concat(ref matrix); c.DrawBitmap(bitmap, 0, 0); return surface.Snapshot();
    }
}
