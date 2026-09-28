namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private Task ShowHelpAsync()
    {
        var panel = new StackPanel { Spacing = 10 };
        foreach (var (title, body) in new[]
        {
            ("Start with your photographs", "Add photos imports JPEG, PNG, WebP, BMP and GIF. Originals remain unchanged. Camera RAW, DNG, HEIF and TIFF decoding are not included."),
            ("Non-destructive development", "Sliders, curves, color mixing/grading, crop, masks and cloning change edit settings, not original bytes. Double-click a slider to reset it or type numeric values. Each completed gesture is one undo transaction; Escape cancels a captured gesture before leaving its tool."),
            ("RGB point curves", "RGB curves opens master, red, green and blue transfer curves. Click to add points, drag to reshape, or enter Input/Output values. Delete removes an interior point. Smooth uses shape-preserving interpolation; Linear uses straight segments. Up to 32 points per channel are supported. The legacy five-point curve is applied first."),
            ("Four-way grading", "Grade opens shadows, midtones, highlights and global color wheels. Drag hue/saturation or use numeric sliders; luminance, blending and balance refine the result. Grading follows monochrome conversion and is preserved in copied settings, versions and exports."),
            ("Gradients and ranges", "Drag radial or linear gradients and edit their center, radius, fade or rotation handles. Luminance selects source brightness; Restrict luminance intersects the range with spatial coverage. Up to eight masks support amount, local tone/color, enable/invert, rename, duplicate, delete and undo."),
            ("Sampled color ranges", "Color range creates a color-selection mask. Click a source color, Shift-click to add up to five samples, or Alt-click a sample pin to remove it. Sample selected colors on an existing mask to restrict its coverage. Tolerance and smoothness use Oklab distance, not Adobe's algorithm. Picking averages a bounded source-preview patch before development; it is not calibrated RAW color or full-resolution sampling."),
            ("Brush masks", "Brush or B selects freehand painting. New brush creates a mask; Paint selected modifies the current one. Expand Brush settings for size, feather, flow and density. Erase subtracts coverage; Alt temporarily erases; brackets change size. Pressure-aware pen input is supported in code. Each stroke is undoable and Escape cancels it. Coverage is capped at a 1024-pixel long edge, including export; physical pen hardware is not certified by CI."),
            ("Coverage, crop and clone", "Coverage shows the selected mask in red and is excluded from export and histogram sampling. Crop supports handles, centered ratios, quarter turns and flips, not arbitrary straightening or perspective correction. Clone uses Alt-click for a source and click for a destination, with up to 32 feathered stamps; it is not healing or generative removal."),
            ("Organize and compare", "Grid/filmstrip and Control-click support selection. Ratings, flags, captions and keywords support filtering; albums reference originals. Drag the comparison divider without creating an edit. Preview decode is capped at 2560 pixels, so source-pixel zoom cannot recover missing full-resolution detail."),
            ("XMP sidecars", "Photo information provides Import XMP and Export XMP. Review supported fields and compatibility warnings before Apply. Metadata only preserves development; a native LightSpace extension can retain all settings. Camera Raw mappings are a limited parameter/curve subset, not Adobe-rendered pixel parity. Sidecars do not alter original images; unknown third-party properties are not retained on re-export."),
            ("Keyboard", "G: grid · E/D: edit · R: crop · M: masks · B: brush · Z: zoom · Y: compare · backslash: original · 0–5: rating · P/X/U: flags · arrows: photo navigation · Ctrl/Cmd+Z: undo · Ctrl/Cmd+Shift+Z: redo · Ctrl/Cmd+I: import · Ctrl/Cmd+S: catalog backup. Focused curve/slider controls use their own editing keys."),
            ("Recovery and compatibility", "The save indicator acknowledges completed committed revisions, never a live preview. Click it to save or retry. Unreadable recovery is protected until explicit replacement. Schemas 1–3 migrate to schema 4, which preserves sampled color ranges alongside brush/curve settings. Older builds reject it rather than silently lose edits. Keep portable backups."),
            ("Incremental storage", "Recovery stores immutable originals by SHA-256 and publishes a separate edit manifest. After the first write, metadata-only saves do not rewrite or rehash unchanged sources. Startup verifies source length and hash; missing or damaged originals protect the previous recovery until explicit replacement. Portable catalog exports still contain original bytes. Unreferenced source files are retained; there is no cross-tab merge, encrypted vault or crash journal."),
            ("Export and privacy", "JPEG/PNG/WebP exports are 8-bit sRGB with an 8192-pixel long-edge cap and no embedded source EXIF/IPTC metadata. Catalog backups retain original bytes. Recovery uses local unencrypted IndexedDB or application data and does not merge concurrent tabs. No account, photo upload or cloud processing is used."),
            ("About LightSpace", "Independent MIT-licensed Uno software, not an Adobe product or complete Lightroom parity. Hardware acceleration depends on the host's Skia surface; there is no separate WebGPU engine. RAW/AI, calibrated profiles, HDR/panorama, tiled native-resolution inspection, printing and cloud workflows remain unimplemented.")
        })
        {
            panel.Children.Add(Theme.Text(title, 13)); panel.Children.Add(Note(body));
        }
        ShowDialog("LightSpace guide", panel, "Close", () => Task.CompletedTask);
        return Task.CompletedTask;
    }
}
