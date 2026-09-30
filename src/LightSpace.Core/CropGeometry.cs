namespace LightSpace.Core;

/// <summary>Analytic crop arithmetic in the corrected image frame, before final quarter-turns/flips.</summary>
public static class CropGeometry
{
    // Keep the existing CropSettings minimum, including a small float-rounding margin.
    public const double MinimumExtent = .0100001;

    public static RectD Bounds(CropSettings crop) => new(crop.Left, crop.Top,
        (double)crop.Right - crop.Left, (double)crop.Bottom - crop.Top);

    public static double OutputAspect(CropSettings crop, int width, int height)
    {
        ValidateDimensions(width, height);
        crop = crop.Normalize();
        var bounds = Bounds(crop);
        var ratio = bounds.Width * width / (bounds.Height * height);
        return (crop.QuarterTurns & 1) == 0 ? ratio : 1 / ratio;
    }

    public static double FrameAspect(double outputAspect, CropSettings crop, int width, int height)
    {
        ValidateDimensions(width, height);
        if (!double.IsFinite(outputAspect) || outputAspect <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputAspect), "The width-to-height ratio must be positive and finite.");
        var pixelAspect = (crop.QuarterTurns & 1) == 0 ? outputAspect : 1 / outputAspect;
        var ratio = pixelAspect * height / width;
        if (ratio < MinimumExtent || ratio > 1 / MinimumExtent)
            throw new ArgumentOutOfRangeException(nameof(outputAspect), "This ratio cannot fit within the existing one-percent crop limits for this image.");
        return ratio;
    }

    /// <summary>Preserve area where possible, clamp size to the frame, and retain the center unless a boundary requires translation.</summary>
    public static CropSettings WithAspect(CropSettings crop, double outputAspect, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(crop);
        crop = crop.Normalize();
        var ratio = FrameAspect(outputAspect, crop, width, height);
        var box = Bounds(crop);
        var h = Math.Sqrt(box.Width * box.Height / ratio);
        h = Math.Clamp(h, Math.Max(MinimumExtent, MinimumExtent / ratio), Math.Min(1, 1 / ratio));
        var w = h * ratio;
        var cx = box.X + box.Width / 2; var cy = box.Y + box.Height / 2;
        return Apply(crop, new(Math.Clamp(cx - w / 2, 0, 1 - w), Math.Clamp(cy - h / 2, 0, 1 - h), w, h));
    }

    /// <summary>Replace only the crop bounds. Matching float bounds preserve object identity and skip redundant previews.</summary>
    public static CropSettings Apply(CropSettings crop, RectD bounds)
    {
        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)
            || bounds.Width < .01 - 1e-6 || bounds.Height < .01 - 1e-6 || bounds.X < -1e-9 || bounds.Y < -1e-9
            || bounds.Right > 1 + 1e-9 || bounds.Bottom > 1 + 1e-9)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Crop bounds must be finite and within the image frame.");
        var l = (float)Math.Clamp(bounds.X, 0, 1); var t = (float)Math.Clamp(bounds.Y, 0, 1);
        var r = (float)Math.Clamp(bounds.Right, 0, 1); var b = (float)Math.Clamp(bounds.Bottom, 0, 1);
        if (crop.Left == l && crop.Top == t && crop.Right == r && crop.Bottom == b) return crop;
        return (crop with { Left = l, Top = t, Right = r, Bottom = b }).Normalize();
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
    }
}
