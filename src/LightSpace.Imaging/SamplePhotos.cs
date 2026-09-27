using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Imaging;

/// <summary>Original generated fallback artwork; no external assets or network calls are needed.</summary>
public static class SamplePhotos
{
    public static CatalogDocument CreateCatalog()
    {
        var catalog = new CatalogDocument();
        string[] names = ["Alpine morning", "Last light", "Blue hour", "Quiet shores", "Autumn ridge", "Northern air", "Still water", "Into the valley"];
        for (var i = 0; i < names.Length; i++)
        {
            var photo = PhotoCodec.Import($"{names[i]}.png", Create(i));
            photo.State = new() { Rating = i == 0 ? 5 : i % 4, Flag = i % 3 == 0 ? PhotoFlag.Pick : PhotoFlag.None, Keywords = ["landscape", "sample", i % 2 == 0 ? "mountains" : "lake"] };
            photo.Camera = "LightSpace sample artwork"; photo.ExposureInfo = "Original procedural landscape · import your photographs to begin";
            catalog.Photos.Add(photo);
        }
        catalog.ActivePhoto = catalog.Photos[0].Id;
        catalog.Albums.Add(new Album { Name = "Alpine stories", Photos = catalog.Photos.Take(5).Select(p => p.Id).ToList() });
        catalog.Albums.Add(new Album { Name = "Quiet moments", Photos = catalog.Photos.Skip(4).Select(p => p.Id).ToList() });
        return catalog;
    }
    public static byte[] Create(int seed)
    {
        const int w = 1440, h = 960;
        using var surface = SKSurface.Create(new SKImageInfo(w, h)); var c = surface.Canvas; var random = new Random(seed + 912);
        using var paint = new SKPaint { IsAntialias = true };
        var warm = seed % 3 == 1;
        using (var sky = SKShader.CreateLinearGradient(new(0, 0), new(0, 690), warm ? [SKColor.Parse("#657d93"), SKColor.Parse("#efc298")] : [SKColor.Parse("#3b7797"), SKColor.Parse("#ccdadd")], null, SKShaderTileMode.Clamp))
        { paint.Shader = sky; c.DrawRect(0, 0, w, h, paint); paint.Shader = null; }
        paint.Color = new SKColor(255, 242, 216, 130); c.DrawCircle(1050 - seed * 35, 245 + seed * 8, 54, paint);
        using var mountains = new SKPictureRecorder(); var mc = mountains.BeginRecording(new(0, 0, w, h));
        for (var layer = 0; layer < 4; layer++)
        {
            var points = new List<SKPoint> { new(-100, 650) };
            for (var x = -100; x <= w + 200; x += 80)
            {
                var ridge = 505 - layer * 12 - Math.Sin((x + seed * 63) * .004 + layer * 1.7) * (70 + layer * 29) - random.Next(20, 95);
                points.Add(new(x, (float)ridge));
            }
            using var path = new SKPath(); path.MoveTo(points[0]); foreach (var p in points.Skip(1)) path.LineTo(p); path.LineTo(w + 200, 680); path.LineTo(-100, 680); path.Close();
            var color = layer switch { 0 => "#a2b6c0", 1 => "#7e99aa", 2 => "#4e6d7a", _ => "#2e4b50" };
            paint.Color = SKColor.Parse(color); mc.DrawPath(path, paint);
            if (layer == 1 || layer == 2)
            {
                for (var j = 1; j < points.Count - 1; j++) if (points[j].Y < points[j - 1].Y && points[j].Y < points[j + 1].Y)
                {
                    using var snow = new SKPath(); var p = points[j]; snow.MoveTo(p); snow.LineTo(p.X + 45, p.Y + 59); snow.LineTo(p.X + 8, p.Y + 38); snow.LineTo(p.X - 12, p.Y + 55); snow.LineTo(p.X - 50, p.Y + 61); snow.Close();
                    paint.Color = SKColor.Parse(layer == 1 ? "#e5e6da" : "#bccdc9"); mc.DrawPath(snow, paint);
                }
            }
        }
        using var picture = mountains.EndRecording(); c.DrawPicture(picture);
        using (var water = SKShader.CreateLinearGradient(new(0, 630), new(0, h), [SKColor.Parse("#557e83"), SKColor.Parse(warm ? "#628d86" : "#214d5a")], null, SKShaderTileMode.Clamp))
        { paint.Shader = water; c.DrawRect(0, 635, w, h - 635, paint); paint.Shader = null; }
        c.Save(); c.ClipRect(new(0, 638, w, h)); c.Translate(0, 1275); c.Scale(1, -1);
        using (var reflection = new SKPaint { Color = SKColors.White.WithAlpha(105) }) c.DrawPicture(picture, reflection); c.Restore();
        for (var i = 0; i < 180; i++) { var y = random.Next(640, h); var x = random.Next(w); paint.Color = new SKColor(192, 218, 219, (byte)random.Next(8, 35)); paint.StrokeWidth = 1; c.DrawLine(x, y, x + random.Next(8, 95), y, paint); }
        for (var i = 0; i < 160; i++)
        {
            var x = random.Next(w); var y = 627 + (float)Math.Sin(x * .008) * 13; var height = random.Next(15, 55);
            paint.Color = SKColor.Parse(i % 2 == 0 ? "#233f38" : "#345346"); using var tree = new SKPath();
            tree.MoveTo(x, y - height); tree.LineTo(x + height * .22f, y); tree.LineTo(x - height * .22f, y); tree.Close(); c.DrawPath(tree, paint);
        }
        paint.Color = SKColor.Parse("#1d3633"); using var foreground = new SKPath(); foreground.MoveTo(0, 830); foreground.CubicTo(130, 790, 140, 940, 380, 960); foreground.LineTo(0, 960); foreground.Close(); c.DrawPath(foreground, paint);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
    }
}
