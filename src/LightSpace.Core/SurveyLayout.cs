namespace LightSpace.Core;

public readonly record struct SurveyImage(Guid Id, double Aspect);
public readonly record struct SurveyTile(Guid Id, RectD Bounds, RectD ImageBounds);

/// <summary>Stable ordered, justified rows. Layout never crops a photo; the caller fits its real aspect inside ImageBounds.</summary>
public static class SurveyLayout
{
    public const int MaximumVisiblePhotos = 12;

    public static SurveyTile[] Arrange(IReadOnlyList<SurveyImage> images, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (!double.IsFinite(width) || !double.IsFinite(height) || width < 0 || height < 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Survey dimensions must be finite and nonnegative.");
        if (images.Count > MaximumVisiblePhotos) throw new ArgumentException("Page the survey before arranging it.", nameof(images));
        if (images.Count == 0 || width < 1 || height < 1) return [];
        if (images.Any(p => p.Id == Guid.Empty || !double.IsFinite(p.Aspect) || p.Aspect <= 0) || images.Select(p => p.Id).Distinct().Count() != images.Count)
            throw new ArgumentException("Survey images require distinct identities and positive finite aspects.", nameof(images));

        var n = images.Count;
        var gap = Math.Min(12, Math.Min(width, height) / (n * 4));
        var minimumWidth = Math.Min(132, Math.Max(1, width / Math.Ceiling(Math.Sqrt(n * width / height)) - gap));
        var columnsAtMinimum = Math.Max(1, (int)Math.Floor((width + gap) / (minimumWidth + gap)));
        var maximumRows = (n + columnsAtMinimum - 1) / columnsAtMinimum;
        var footer = Math.Min(48, Math.Max(0, (height - gap * (maximumRows - 1)) / maximumRows * .4));
        var aspects = images.Select(p => Math.Clamp(p.Aspect, 1d / 12, 12)).ToArray();

        int Rows(double h)
        {
            var rows = 1; var used = 0d;
            foreach (var aspect in aspects)
            {
                var w = Math.Max(minimumWidth, aspect * h);
                if (w > width + 1e-7) return n + 1;
                if (used > 0 && used + gap + w > width + 1e-7) { rows++; used = w; }
                else used += (used == 0 ? 0 : gap) + w;
            }
            return rows;
        }
        var low = 0d; var high = height;
        // Feasibility is monotone: wider/taller tiles never require fewer rows.
        for (var iteration = 0; iteration < 44; iteration++)
        {
            var h = (low + high) / 2; var rows = Rows(h);
            if (rows <= n && rows * (h + footer) + (rows - 1) * gap <= height) low = h;
            else high = h;
        }
        var imageHeight = low; var rowCount = Rows(low);
        var y = Math.Max(0, (height - rowCount * (imageHeight + footer) - (rowCount - 1) * gap) / 2);
        var result = new SurveyTile[n]; var start = 0;
        while (start < n)
        {
            var end = start; var rowWidth = 0d;
            while (end < n)
            {
                var w = Math.Min(width, Math.Max(minimumWidth, aspects[end] * imageHeight));
                if (end > start && rowWidth + gap + w > width + 1e-7) break;
                rowWidth += (end == start ? 0 : gap) + w; end++;
            }
            var x = Math.Max(0, (width - rowWidth) / 2);
            for (var index = start; index < end; index++)
            {
                var w = Math.Min(width - x, Math.Max(minimumWidth, aspects[index] * imageHeight));
                var h = Math.Min(height - y, imageHeight + footer);
                result[index] = new(images[index].Id, new(x, y, w, h), new(x, y, w, Math.Min(imageHeight, h)));
                x += w + gap;
            }
            start = end; y += imageHeight + footer + gap;
        }
        return result;
    }
}
