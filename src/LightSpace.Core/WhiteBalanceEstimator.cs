namespace LightSpace.Core;

public sealed record WhiteBalanceEstimate(float Temperature, float Tint, bool WasClamped);

/// <summary>Neutralizes a sampled source under LightSpace's relative channel-gain model, not camera Kelvin calibration.</summary>
public static class WhiteBalanceEstimator
{
    public static WhiteBalanceEstimate? Estimate(ColorSample sample)
    {
        static double Linear(float value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
        var s = sample.Normalize(); var r = Linear(s.Red); var g = Linear(s.Green); var b = Linear(s.Blue);
        if (Math.Min(g, r + b) < .00001) return null;
        var temperature = (b - r) / (.22 * (r + b)); var t = Math.Clamp(temperature, -1, 1);
        var tint = ((r * (1 + .22 * t) + b * (1 - .22 * t)) * .5 / g - 1) / .12;
        return new((float)t * 100, (float)Math.Clamp(tint, -1, 1) * 100, Math.Abs(temperature) > 1 || Math.Abs(tint) > 1);
    }
}
