using System.Runtime.CompilerServices;
namespace LightSpace.Core;

public readonly record struct BrushDab(float X, float Y, float Pressure = 1)
{
    public BrushDab Normalize() => new(Numeric.Unit(X), Numeric.Unit(Y), Numeric.Clamp(Pressure, .05f, 1));
}

/// <summary>Immutable resampled stroke. Radius is measured in source-image-height units.</summary>
public sealed record BrushStroke
{
    public const int MaximumDabs = 4096;
    public const int MaximumStrokes = 64;
    private static readonly ConditionalWeakTable<BrushDab[], BrushDab[]> NormalizedDabs = new();
    public Guid Id { get; init; } = Guid.NewGuid();
    public BrushDab[] Dabs { get; init; } = [];
    public float Radius { get; init; } = .04f;
    public float Feather { get; init; } = .7f;
    public float Flow { get; init; } = .35f;
    public float Density { get; init; } = 1;
    public bool Erase { get; init; }

    public BrushStroke Normalize()
    {
        if (Dabs is { Length: > MaximumDabs }) throw new InvalidDataException($"A stroke cannot contain more than {MaximumDabs} dabs.");
        var dabs = Dabs is null ? [] : NormalizedDabs.GetValue(Dabs, static input =>
        {
            BrushDab[]? repaired = null;
            for (var i = 0; i < input.Length; i++)
            {
                var dab = input[i].Normalize(); if (dab == input[i]) continue;
                repaired ??= (BrushDab[])input.Clone(); repaired[i] = dab;
            }
            return repaired ?? input;
        });
        var result = this with { Dabs = dabs, Radius = Numeric.Clamp(Radius, .002f, .25f), Feather = Numeric.Unit(Feather), Flow = Numeric.Unit(Flow), Density = Numeric.Unit(Density) };
        return result == this ? this : result;
    }
    public bool SameParameters(BrushStroke other) => Radius == other.Radius && Feather == other.Feather && Flow == other.Flow && Density == other.Density && Erase == other.Erase;
    public bool PixelEquals(BrushStroke other) => ReferenceEquals(this, other) || SameParameters(other) && Dabs.AsSpan().SequenceEqual(other.Dabs);
    public static BrushStroke[] NormalizeAll(BrushStroke[]? strokes)
    {
        if (strokes is null) return [];
        if (strokes.Length > MaximumStrokes) throw new InvalidDataException($"A mask cannot contain more than {MaximumStrokes} brush strokes.");
        BrushStroke[]? repaired = null; var count = 0;
        for (var i = 0; i < strokes.Length; i++)
        {
            if (strokes[i] is null) throw new InvalidDataException("A mask contains an empty brush stroke.");
            var stroke = strokes[i].Normalize(); count += stroke.Dabs.Length;
            if (count > 65536) throw new InvalidDataException("A mask exceeds the 65,536-dab safety limit.");
            if (ReferenceEquals(stroke, strokes[i])) continue;
            repaired ??= (BrushStroke[])strokes.Clone(); repaired[i] = stroke;
        }
        return repaired ?? strokes;
    }
    public static bool SequenceEquals(BrushStroke[] a, BrushStroke[] b)
    {
        if (ReferenceEquals(a, b)) return true; if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) if (!a[i].PixelEquals(b[i])) return false;
        return true;
    }
    public static float Apply(float initialCoverage, BrushStroke[] strokes, float x, float y, float aspect)
    {
        var coverage = Numeric.Unit(initialCoverage);
        foreach (var stroke in strokes)
        {
            var amount = 0f;
            foreach (var dab in stroke.Dabs)
            {
                var dx = (x - dab.X) * aspect; var dy = y - dab.Y;
                var radius = stroke.Radius * (.2f + .8f * dab.Pressure);
                var d = MathF.Sqrt(dx * dx + dy * dy) / radius;
                var weight = stroke.Feather <= 0 ? (d <= 1 ? 1 : 0) : 1 - Numeric.Smooth(1 - stroke.Feather, 1, d);
                amount += (stroke.Density - amount) * stroke.Flow * dab.Pressure * weight;
            }
            coverage = stroke.Erase ? coverage * (1 - amount) : coverage + (1 - coverage) * amount;
        }
        return Numeric.Unit(coverage);
    }
}

/// <summary>Arc-length resampling is independent of pointer-event frequency.</summary>
public sealed class BrushStrokeBuilder
{
    private readonly BrushStroke _settings;
    private readonly float _aspect, _spacing;
    private readonly List<BrushDab> _dabs = [];
    private BrushDab? _previous;
    private float _remaining;
    public bool IsFull => _dabs.Count >= BrushStroke.MaximumDabs;
    public int Count => _dabs.Count;
    public BrushStrokeBuilder(BrushStroke settings, float aspect)
    {
        if (!float.IsFinite(aspect) || aspect <= 0) throw new ArgumentOutOfRangeException(nameof(aspect));
        _settings = settings.Normalize() with { Dabs = [] }; _aspect = aspect;
        _spacing = _settings.Radius * .25f; _remaining = _spacing;
    }
    public void Add(float x, float y, float pressure = 1)
    {
        var point = new BrushDab(x, y, pressure).Normalize();
        if (_previous is not { } previous) { _previous = point; _dabs.Add(point); return; }
        var dx = (point.X - previous.X) * _aspect; var dy = point.Y - previous.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length > .000001f)
        {
            var distance = _remaining;
            while (distance <= length + .000001f && !IsFull)
            {
                var t = Math.Clamp(distance / length, 0, 1);
                _dabs.Add(new(previous.X + (point.X - previous.X) * t, previous.Y + (point.Y - previous.Y) * t, previous.Pressure + (point.Pressure - previous.Pressure) * t));
                distance += _spacing;
            }
            _remaining = Math.Max(.000001f, distance - length);
        }
        _previous = point;
    }
    public BrushStroke Snapshot() => _settings with { Dabs = _dabs.ToArray() };
}
