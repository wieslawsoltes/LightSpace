using LightSpace.Core;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private string? _maskAction;
    public IReadOnlyList<ViewportHandle> InteractionHandles()
    {
        var handles = new List<ViewportHandle>();
        if (Compare && Tool == PhotoTool.Edit) handles.Add(new("compare-divider", _imageRect.Left + _imageRect.Width * ComparisonPosition, _imageRect.MidY));
        var photo = _session.Active;
        if (!IsMaskTool || Tool == PhotoTool.Brush || !MaskOverlay || photo is null || ActiveMask < 0 || ActiveMask >= photo.State.Masks.Length) return handles;
        var m = photo.State.Masks[ActiveMask]; if (m.Kind is MaskKind.LuminanceRange or MaskKind.Brush) return handles;
        var aspect = (float)photo.Width / photo.Height;
        void Add(string id, float x, float y)
        {
            var uv = m.LocalToSource(x, y, aspect); var p = ToView((float)uv.X, (float)uv.Y); handles.Add(new(id, p.X, p.Y));
        }
        Add("mask-center", 0, 0);
        if (m.Kind == MaskKind.Linear) { Add("mask-fade-start", 0, -m.RadiusY); Add("mask-fade-end", 0, m.RadiusY); }
        else
        {
            Add("mask-radius-x", m.RadiusX * aspect, 0); Add("mask-radius-y", 0, m.RadiusY);
            Add("mask-rotation", 0, -m.RadiusY - .05f);
        }
        return handles;
    }
    private string? HitMask(Point screen)
    {
        if (!MaskOverlay || _session.Active is not { } photo) return null;
        bool Near(float x, float y) => Math.Abs(x - screen.X) <= 12 && Math.Abs(y - screen.Y) <= 12;
        foreach (var handle in InteractionHandles().Reverse()) if (Near(handle.X, handle.Y)) return handle.Id;
        for (var i = photo.State.Masks.Length - 1; i >= 0; i--)
        {
            var mask = photo.State.Masks[i]; if (mask.Kind is MaskKind.LuminanceRange or MaskKind.Brush) continue;
            var p = ToView(mask.X, mask.Y); if (!Near(p.X, p.Y)) continue;
            ActiveMask = i; ViewChanged?.Invoke(); return "mask-center";
        }
        return null;
    }
    private void MoveMask(SKPoint p, PhotoState start)
    {
        if (_session.Active is not { } photo) return;
        var aspect = (float)photo.Width / photo.Height;
        if (_maskAction is not null && ActiveMask >= 0 && ActiveMask < start.Masks.Length)
        {
            var mask = start.Masks[ActiveMask]; var dx = (p.X - mask.X) * aspect; var dy = p.Y - mask.Y;
            var a = mask.Angle * MathF.PI / 180; var cos = MathF.Cos(a); var sin = MathF.Sin(a);
            var qx = cos * dx + sin * dy; var qy = -sin * dx + cos * dy;
            var changed = _maskAction switch
            {
                "mask-center" => mask with { X = mask.X + p.X - _press.X, Y = mask.Y + p.Y - _press.Y },
                "mask-radius-x" => mask with { RadiusX = MathF.Abs(qx) / aspect },
                "mask-radius-y" => mask with { RadiusY = MathF.Abs(qy) },
                "mask-rotation" => mask with { Angle = MathF.Atan2(dx, -dy) * 180 / MathF.PI },
                "mask-fade-start" => mask with { RadiusY = MathF.Sqrt(dx * dx + dy * dy), Angle = MathF.Atan2(dx, -dy) * 180 / MathF.PI },
                "mask-fade-end" => mask with { RadiusY = MathF.Sqrt(dx * dx + dy * dy), Angle = MathF.Atan2(-dx, dy) * 180 / MathF.PI },
                _ => mask
            };
            _session.Preview(s => s with { Masks = s.Masks.Select((m, i) => i == ActiveMask ? changed : m).ToArray() });
            return;
        }
        var deltaX = (p.X - _press.X) * aspect; var deltaY = p.Y - _press.Y;
        if (MathF.Abs(deltaX) + MathF.Abs(deltaY) < .006f) return;
        var linear = Tool == PhotoTool.LinearMask;
        var m = new LocalMask
        {
            Id = _maskId, Name = (linear ? "Linear gradient " : "Radial gradient ") + (start.Masks.Length + 1),
            Kind = linear ? MaskKind.Linear : MaskKind.Radial, X = (_press.X + p.X) / 2, Y = (_press.Y + p.Y) / 2,
            RadiusX = Math.Max(.005f, Math.Abs(p.X - _press.X) / 2),
            RadiusY = linear ? MathF.Sqrt(deltaX * deltaX + deltaY * deltaY) / 2 : Math.Max(.005f, Math.Abs(deltaY) / 2),
            Angle = linear ? Numeric.Angle(MathF.Atan2(-deltaX, deltaY) * 180 / MathF.PI) : 0
        };
        _session.Preview(s => s with { Masks = [.. start.Masks, m] });
    }
    private void PaintMasks(SKCanvas canvas, PhotoDocument photo)
    {
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        var aspect = (float)photo.Width / photo.Height;
        canvas.Save(); canvas.ClipRect(_imageRect);
        for (var i = 0; i < photo.State.Masks.Length; i++)
        {
            var m = photo.State.Masks[i]; if (m.Kind is MaskKind.LuminanceRange or MaskKind.Brush) continue;
            paint.Color = i == ActiveMask ? SKColor.Parse("#b3dcff") : SKColors.White.WithAlpha(m.Enabled ? (byte)180 : (byte)70);
            SKPoint Map(float x, float y) { var p = m.LocalToSource(x, y, aspect); return ToView((float)p.X, (float)p.Y); }
            if (m.Kind == MaskKind.Radial)
            {
                using var path = new SKPath();
                for (var step = 0; step <= 64; step++)
                {
                    var angle = step * MathF.PI / 32; var point = Map(MathF.Cos(angle) * m.RadiusX * aspect, MathF.Sin(angle) * m.RadiusY);
                    if (step == 0) path.MoveTo(point); else path.LineTo(point);
                }
                path.Close(); canvas.DrawPath(path, paint);
            }
            else
            {
                foreach (var y in new[] { -m.RadiusY, 0, m.RadiusY }) canvas.DrawLine(Map(-aspect * 2, y), Map(aspect * 2, y), paint);
                canvas.DrawLine(Map(0, -m.RadiusY), Map(0, m.RadiusY), paint);
            }
            canvas.DrawCircle(Map(0, 0), 5, paint);
        }
        canvas.Restore();
        foreach (var handle in InteractionHandles())
        {
            paint.Style = SKPaintStyle.Fill; paint.Color = SKColor.Parse("#242424"); canvas.DrawCircle(handle.X, handle.Y, 5, paint);
            paint.Style = SKPaintStyle.Stroke; paint.Color = SKColor.Parse("#b3dcff"); canvas.DrawCircle(handle.X, handle.Y, 5, paint);
        }
    }
}
