using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public sealed partial class PhotoRenderer
{
    private static void ConfigureUniforms(SKRuntimeEffectUniforms u, SKImage image, PhotoState state,
        int sourceWidth, int sourceHeight, int brushWidth, int brushHeight, int overlayMask)
    {
        var s = state.Develop;
        u["size"] = new float[] { image.Width, image.Height };
        u["sourceSize"] = new float[] { sourceWidth > 0 ? sourceWidth : image.Width, sourceHeight > 0 ? sourceHeight : image.Height };
        u["brushSize"] = new float[] { brushWidth, brushHeight };
        u["useChannels"] = s.Channels.IsIdentity ? 0 : 1;
        u["light"] = new[] { s.Exposure, s.Contrast / 100, s.Highlights / 100, s.Shadows / 100 };
        u["tone"] = new[] { s.Whites / 100, s.Blacks / 100, s.Temperature / 100, s.Tint / 100 };
        u["color"] = new[] { s.Vibrance / 100, s.Saturation / 100, s.Monochrome ? 1f : 0, s.Dehaze / 100 };
        u["detail"] = new[] { s.Texture / 100, s.Clarity / 100, s.Sharpening / 100, s.NoiseReduction / 100 };
        u["effects"] = new[] { s.Vignette / 100, s.Grain / 100 };
        u["curve"] = new[] { s.Curve.Black, s.Curve.Shadow, s.Curve.Mid, s.Curve.Light };
        u["curveWhite"] = s.Curve.White;
        u["useCurve"] = s.Curve.IsIdentity ? 0 : 1;
        u["useMixer"] = s.Mixer.Any(b => b.Hue != 0 || b.Saturation != 0 || b.Luminance != 0) ? 1 : 0;
        u["mixer"] = s.Mixer.SelectMany(b => new[] { b.Hue / 100, b.Saturation / 100, b.Luminance / 100, 0 }).ToArray();
        u["useGrading"] = s.Grading.IsNeutral ? 0 : 1;
        u["gradeMix"] = new[] { s.Grading.Blending / 100, s.Grading.Balance / 100 };
        u["overlayMask"] = overlayMask;
        var grading = new float[16];
        for (var i = 0; i < 4; i++)
        {
            var grade = s.Grading.Get((GradingRange)i); var v = grade.TintVector();
            grading[i * 4] = v.X; grading[i * 4 + 1] = v.Y;
            grading[i * 4 + 2] = v.Z; grading[i * 4 + 3] = grade.Luminance / 100;
        }
        u["grading"] = grading;
        var geometry = new float[32]; var adjustments = new float[32];
        var controls = new float[32]; var ranges = new float[32]; var extra = new float[32];
        for (var i = 0; i < state.Masks.Length; i++)
        {
            var m = state.Masks[i]; var a = m.Angle * MathF.PI / 180;
            new[] { m.X, m.Y, m.RadiusX, m.RadiusY }.CopyTo(geometry, i * 4);
            new[] { m.Exposure, m.Saturation / 100, m.Feather, m.Inverted ? 1f : 0 }.CopyTo(adjustments, i * 4);
            new[] { MathF.Cos(a), MathF.Sin(a), (float)m.Kind, m.Enabled ? m.Opacity : 0 }.CopyTo(controls, i * 4);
            new[] { m.RangeMin, m.RangeMax, m.RangeSmoothness, m.RangeEnabled || m.Kind == MaskKind.LuminanceRange ? 1f : 0 }.CopyTo(ranges, i * 4);
            new[] { m.Contrast / 100, m.Temperature / 100, m.Tint / 100, m.Strokes.Length > 0 ? 1f : 0 }.CopyTo(extra, i * 4);
        }
        u["maskCount"] = state.Masks.Length; u["maskGeometry"] = geometry; u["maskAdjust"] = adjustments;
        SetColorUniforms(u, state.Masks);
        u["maskControl"] = controls; u["maskRange"] = ranges; u["maskExtra"] = extra;
        var spotData = new float[128]; var radii = new float[32];
        for (var i = 0; i < state.CloneSpots.Length; i++)
        {
            var p = state.CloneSpots[i];
            new[] { p.X, p.Y, p.SourceX, p.SourceY }.CopyTo(spotData, i * 4); radii[i] = p.Radius;
        }
        u["spotCount"] = state.CloneSpots.Length; u["spots"] = spotData; u["radii"] = radii;
    }
}
