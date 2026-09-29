namespace LightSpace.Core;

/// <summary>Non-destructive projective framing in orientation-normalized image coordinates.</summary>
public sealed record GeometrySettings
{
    public float Rotate { get; init; }
    public float Vertical { get; init; }
    public float Horizontal { get; init; }
    public float Aspect { get; init; }
    public float Scale { get; init; } = 100;
    public float XOffset { get; init; }
    public float YOffset { get; init; }
    public bool ConstrainCrop { get; init; }
    public bool IsIdentity => Rotate == 0 && Vertical == 0 && Horizontal == 0 && Aspect == 0
        && Scale == 100 && XOffset == 0 && YOffset == 0;
    public GeometrySettings Normalize()
    {
        var value = this with
        {
            Rotate = Numeric.Clamp(Rotate, -45, 45), Vertical = DevelopSettings.Percent(Vertical),
            Horizontal = DevelopSettings.Percent(Horizontal), Aspect = DevelopSettings.Percent(Aspect),
            Scale = float.IsFinite(Scale) ? Math.Clamp(Scale, 50, 200) : 100,
            XOffset = DevelopSettings.Percent(XOffset), YOffset = DevelopSettings.Percent(YOffset)
        };
        return value == this ? this : value;
    }
    public float Get(string name) => name switch
    {
        nameof(Rotate) => Rotate, nameof(Vertical) => Vertical, nameof(Horizontal) => Horizontal,
        nameof(Aspect) => Aspect, nameof(Scale) => Scale, nameof(XOffset) => XOffset, nameof(YOffset) => YOffset,
        _ => throw new ArgumentException("Unknown geometry adjustment: " + name, nameof(name))
    };
    public GeometrySettings Set(string name, float value) => (name switch
    {
        nameof(Rotate) => this with { Rotate = value }, nameof(Vertical) => this with { Vertical = value },
        nameof(Horizontal) => this with { Horizontal = value }, nameof(Aspect) => this with { Aspect = value },
        nameof(Scale) => this with { Scale = value }, nameof(XOffset) => this with { XOffset = value },
        nameof(YOffset) => this with { YOffset = value },
        _ => throw new ArgumentException("Unknown geometry adjustment: " + name, nameof(name))
    }).Normalize();
}
