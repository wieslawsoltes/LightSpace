namespace LightSpace.Core;

/// <summary>View-only zoom relative to fit, with pan as fractions of the viewport. Never part of photo processing or recovery.</summary>
public readonly record struct PhotoNavigationState(float Zoom = 1, float PanX = 0, float PanY = 0)
{
    public PhotoNavigationState Normalize() => new(Numeric.Clamp(Zoom, .01f, 64), Numeric.Clamp(PanX, -64, 64), Numeric.Clamp(PanY, -64, 64));
    public static PhotoNavigationState Fit => new(1, 0, 0);
}
