namespace LightSpace.Core;

/// <summary>Double-precision column-vector homography, independent of UI and graphics APIs.</summary>
public readonly record struct ProjectiveTransform(double A, double B, double C, double D, double E, double F, double G, double H, double I)
{
    public static ProjectiveTransform Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);
    public static ProjectiveTransform Translate(double x, double y) => new(1, 0, x, 0, 1, y, 0, 0, 1);
    public static ProjectiveTransform Scale(double x, double y) => new(x, 0, 0, 0, y, 0, 0, 0, 1);
    public PointD Map(PointD point)
    {
        var w = G * point.X + H * point.Y + I;
        return Math.Abs(w) < 1e-12 ? new(double.NaN, double.NaN) : new((A * point.X + B * point.Y + C) / w, (D * point.X + E * point.Y + F) / w);
    }
    public bool TryInvert(out ProjectiveTransform result)
    {
        var a = E * I - F * H; var b = C * H - B * I; var c = B * F - C * E;
        var d = F * G - D * I; var e = A * I - C * G; var f = C * D - A * F;
        var g = D * H - E * G; var h = B * G - A * H; var i = A * E - B * D;
        var determinant = A * a + B * d + C * g;
        if (!double.IsFinite(determinant) || Math.Abs(determinant) < 1e-12) { result = default; return false; }
        result = new(a / determinant, b / determinant, c / determinant, d / determinant, e / determinant,
            f / determinant, g / determinant, h / determinant, i / determinant); return true;
    }
    public static ProjectiveTransform operator *(ProjectiveTransform x, ProjectiveTransform y) => new(
        x.A * y.A + x.B * y.D + x.C * y.G, x.A * y.B + x.B * y.E + x.C * y.H, x.A * y.C + x.B * y.F + x.C * y.I,
        x.D * y.A + x.E * y.D + x.F * y.G, x.D * y.B + x.E * y.E + x.F * y.H, x.D * y.C + x.E * y.F + x.F * y.I,
        x.G * y.A + x.H * y.D + x.I * y.G, x.G * y.B + x.H * y.E + x.I * y.H, x.G * y.C + x.H * y.F + x.I * y.I);
}

public static class GeometryProjection
{
    private static readonly PointD[] Corners = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
    public static ProjectiveTransform Create(GeometrySettings settings, LensCorrectionSettings optics, double aspect)
    {
        if (!double.IsFinite(aspect) || aspect <= 0) throw new ArgumentOutOfRangeException(nameof(aspect));
        var g = settings.Normalize(); var result = Build(g, aspect, 1);
        if (!g.ConstrainCrop) return result;
        var extent = LensMapping.SafeExtent(optics);
        if (Covers(result, extent)) return result;
        double low = 1, high = 2;
        while (high < 1048576 && !Covers(Build(g, aspect, high), extent)) { low = high; high *= 2; }
        if (!Covers(Build(g, aspect, high), extent)) throw new InvalidOperationException("The requested constrained geometry exceeds the supported projection range.");
        for (var i = 0; i < 40; i++)
        {
            var middle = (low + high) * .5;
            if (Covers(Build(g, aspect, middle), extent)) high = middle; else low = middle;
        }
        return Build(g, aspect, high * 1.000001);
    }
    private static ProjectiveTransform Build(GeometrySettings g, double aspect, double extraScale)
    {
        var angle = g.Rotate * Math.PI / 180; var c = Math.Cos(angle); var s = Math.Sin(angle);
        var rotate = new ProjectiveTransform(c, -s, 0, s, c, 0, 0, 0, 1);
        var perspective = new ProjectiveTransform(1, 0, 0, 0, 1, 0, g.Horizontal * .005 / aspect, g.Vertical * .005, 1);
        var scale = g.Scale * .01 * extraScale;
        return ProjectiveTransform.Translate(.5 + g.XOffset / 400, .5 + g.YOffset / 400)
            * ProjectiveTransform.Scale(1 / aspect, 1) * rotate
            * ProjectiveTransform.Scale(scale * Math.Pow(2, g.Aspect / 200), scale)
            * perspective * ProjectiveTransform.Scale(aspect, 1) * ProjectiveTransform.Translate(-.5, -.5);
    }
    private static bool Covers(ProjectiveTransform transform, double extent)
    {
        if (!transform.TryInvert(out var inverse)) return false;
        var margin = (1 - extent) * .5;
        foreach (var corner in Corners)
        {
            var p = inverse.Map(corner);
            if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || p.X < margin || p.X > 1 - margin || p.Y < margin || p.Y > 1 - margin) return false;
        }
        return true;
    }
    public static float HorizonCorrection(PointD start, PointD end, double aspect)
    {
        var angle = Math.Atan2(end.Y - start.Y, (end.X - start.X) * aspect) * 180 / Math.PI;
        while (angle > 90) angle -= 180;
        while (angle < -90) angle += 180;
        return (float)Math.Clamp(-angle, -45, 45);
    }
}
