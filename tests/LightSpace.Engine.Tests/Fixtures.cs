using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;

internal static class Fixtures
{
    public static void Check(bool value, string message = "Assertion failed") { if (!value) throw new InvalidOperationException(message); }
    public static void Throws(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected invalid data rejection."); }
    public static PhotoDocument Tiny()
    {
        using var surface = SKSurface.Create(new SKImageInfo(96, 64)); using var p = new SKPaint();
        for (var y = 0; y < 64; y++) for (var x = 0; x < 96; x++) { p.Color = new SKColor((byte)(35 + x * 2), (byte)(35 + y * 3), (byte)(70 + x)); surface.Canvas.DrawRect(x, y, 1, 1, p); }
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return PhotoCodec.Import("fixture.png", data.ToArray());
    }
    public static PhotoDocument Solid(SKColor color, int width = 96, int height = 64)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height)); surface.Canvas.Clear(color);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return PhotoCodec.Import("solid.png", data.ToArray());
    }
    public static CatalogDocument Catalog(int count = 2)
    {
        var c = new CatalogDocument(); for (var i = 0; i < count; i++) { var p = Tiny(); p.Name = $"Photo {i}"; c.Photos.Add(p); }
        c.ActivePhoto = c.Photos[0].Id; return c;
    }
}
