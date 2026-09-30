namespace LightSpace.Core;

public enum CropHandle { Create, Move, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

/// <summary>
/// An immutable gesture anchor. Evaluate is allocation-free, history-independent,
/// and projects constrained corner motion in image-pixel rather than normalized-square distance.
/// </summary>
public readonly struct CropGesture
{
    private readonly RectD _start;
    private readonly PointD _press;
    private readonly CropHandle _handle;
    private readonly bool _locked, _centered;
    private readonly double _ratio, _aspectSquared;
    private readonly int _sx, _sy;

    public CropGesture(CropSettings crop, CropHandle handle, PointD press, int width, int height,
        bool aspectLocked = false, bool fromCenter = false)
    {
        ArgumentNullException.ThrowIfNull(crop);
        CropGeometry.ValidateDimensions(width, height);
        if (!Enum.IsDefined(handle)) throw new ArgumentOutOfRangeException(nameof(handle));
        if (!double.IsFinite(press.X) || !double.IsFinite(press.Y)) throw new ArgumentOutOfRangeException(nameof(press));
        _start = CropGeometry.Bounds(crop.Normalize()); _press = press; _handle = handle;
        _locked = aspectLocked; _centered = fromCenter;
        _ratio = _start.Width / _start.Height;
        _aspectSquared = (double)width / height * width / height;
        (_sx, _sy) = handle switch
        {
            CropHandle.TopLeft => (-1, -1), CropHandle.Top => (0, -1), CropHandle.TopRight => (1, -1),
            CropHandle.Right => (1, 0), CropHandle.BottomRight => (1, 1), CropHandle.Bottom => (0, 1),
            CropHandle.BottomLeft => (-1, 1), CropHandle.Left => (-1, 0), _ => (0, 0)
        };
    }

    public RectD Evaluate(PointD pointer)
    {
        if (!double.IsFinite(pointer.X) || !double.IsFinite(pointer.Y) || _start.Width <= 0 || _start.Height <= 0) return _start;
        var dx = pointer.X - _press.X; var dy = pointer.Y - _press.Y;
        if (dx == 0 && dy == 0) return _start;
        if (_handle == CropHandle.Move)
            return _start with { X = _start.X + Math.Clamp(dx, -_start.X, 1 - _start.Right),
                Y = _start.Y + Math.Clamp(dy, -_start.Y, 1 - _start.Bottom) };
        if (_handle == CropHandle.Create)
        {
            if (_press.X < 0 || _press.X > 1 || _press.Y < 0 || _press.Y > 1) return _start;
            return Corner(_press.X, _press.Y, Math.Abs(dx), Math.Abs(dy), dx < 0 ? -1 : 1, dy < 0 ? -1 : 1, _centered);
        }
        var cx = _start.X + _start.Width / 2; var cy = _start.Y + _start.Height / 2;
        if (_sx != 0 && _sy != 0)
        {
            var ax = _centered ? cx : _sx < 0 ? _start.Right : _start.X;
            var ay = _centered ? cy : _sy < 0 ? _start.Bottom : _start.Y;
            var x = _sx < 0 ? _start.X : _start.Right; var y = _sy < 0 ? _start.Y : _start.Bottom;
            return Corner(ax, ay, _sx * (x + dx - ax), _sy * (y + dy - ay), _sx, _sy, _centered);
        }
        var factor = _centered ? 2d : 1d;
        var w = _start.Width; var h = _start.Height;
        if (_sx != 0)
        {
            var ax = _centered ? cx : _sx < 0 ? _start.Right : _start.X;
            var maxWidth = _centered ? 2 * Math.Min(cx, 1 - cx) : _sx < 0 ? ax : 1 - ax;
            var minWidth = Math.Min(CropGeometry.MinimumExtent, _start.Width);
            if (_locked)
            {
                maxWidth = Math.Min(maxWidth, 2 * Math.Min(cy, 1 - cy) * _ratio);
                minWidth = Math.Max(minWidth, Math.Min(CropGeometry.MinimumExtent, _start.Height) * _ratio);
            }
            if (maxWidth < minWidth) return _start;
            w = Math.Clamp(w + _sx * dx * factor, minWidth, maxWidth);
            if (_locked) h = w / _ratio;
            return new(_centered ? cx - w / 2 : _sx < 0 ? ax - w : ax, cy - h / 2, w, h);
        }
        if (_sy != 0)
        {
            var ay = _centered ? cy : _sy < 0 ? _start.Bottom : _start.Y;
            var maxHeight = _centered ? 2 * Math.Min(cy, 1 - cy) : _sy < 0 ? ay : 1 - ay;
            var minHeight = Math.Min(CropGeometry.MinimumExtent, _start.Height);
            if (_locked)
            {
                maxHeight = Math.Min(maxHeight, 2 * Math.Min(cx, 1 - cx) / _ratio);
                minHeight = Math.Max(minHeight, Math.Min(CropGeometry.MinimumExtent, _start.Width) / _ratio);
            }
            if (maxHeight < minHeight) return _start;
            h = Math.Clamp(h + _sy * dy * factor, minHeight, maxHeight);
            if (_locked) w = h * _ratio;
            return new(cx - w / 2, _centered ? cy - h / 2 : _sy < 0 ? ay - h : ay, w, h);
        }
        return _start;
    }

    private RectD Corner(double ax, double ay, double desiredWidth, double desiredHeight, int sx, int sy, bool centered)
    {
        var factor = centered ? 2d : 1d;
        var maxW = centered ? Math.Min(ax, 1 - ax) : sx < 0 ? ax : 1 - ax;
        var maxH = centered ? Math.Min(ay, 1 - ay) : sy < 0 ? ay : 1 - ay;
        var minW = Math.Min(CropGeometry.MinimumExtent, _start.Width) / factor;
        var minH = Math.Min(CropGeometry.MinimumExtent, _start.Height) / factor;
        double w, h;
        if (_locked)
        {
            var lower = Math.Max(minH, minW / _ratio); var upper = Math.Min(maxH, maxW / _ratio);
            if (upper < lower) return _start;
            h = Math.Clamp((_aspectSquared * _ratio * desiredWidth + desiredHeight) / (_aspectSquared * _ratio * _ratio + 1), lower, upper);
            w = h * _ratio;
        }
        else
        {
            if (maxW < minW || maxH < minH) return _start;
            w = Math.Clamp(desiredWidth, minW, maxW); h = Math.Clamp(desiredHeight, minH, maxH);
        }
        return new(centered || sx < 0 ? ax - w : ax, centered || sy < 0 ? ay - h : ay, w * factor, h * factor);
    }
}
