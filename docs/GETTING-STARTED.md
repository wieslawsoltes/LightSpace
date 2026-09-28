# Getting started

## Open and organize

Launch the browser or native host. LightSpace restores its last successfully written local recovery catalog or opens demonstration photographs. Add photos imports JPEG, PNG, WebP, BMP or the first GIF frame; RAW, DNG, HEIF and TIFF are not decoded.

Select a photograph in the grid or filmstrip. Control-click extends selection. Search matches filename, caption and keyword tokens; Favorites includes four/five-star photographs. Picks and Rejected filter flags. Albums contain references rather than duplicate originals. The grid and filmstrip show pages of up to 60 photos; breadcrumb arrows change pages, and keyboard arrows navigate the filtered sequence.

## Develop light and color

The Edit inspector contains Light, legacy Point curve, Color, Color mixer, Color grading, Effects and Detail sections. Drag sliders, use keyboard arrows when focused or type numeric values. Double-click resets a slider. A completed gesture is one undo transaction; Escape cancels an active gesture.

Temperature/tint are relative adjustments, not calibrated camera Kelvin values. Auto is a luminance heuristic. Presets replace global development while preserving crop and local masks. Copy/Paste and Sync apply processing to selected photos while preserving their individual ratings, flags, captions and keywords.

**Grade** opens shadows/midtones/highlights/global wheels. Hue, saturation, luminance, blending and balance are editable numerically or through the wheel. **RGB curves** opens arbitrary-point master/R/G/B curves. Click to add, drag or type to move, Delete to remove an interior point, and choose Smooth or Linear. The existing legacy five-point curve is applied first. See [grading and masks](COLOR-AND-MASKS.md) and [curve interpolation](ADVANCED-EDITING.md#rgb-point-curves).

## Crop, paint and refine

Crop supports a source-normalized rectangle, edge/corner handles, movement, common centered ratios, quarter turns and flips. Done returns to editing. The unrotated source is displayed during crop editing. Arbitrary-angle straightening and perspective correction are not implemented.

Masking provides rotatable radial/linear gradients and luminance-range selections. Drag gradient handles to adjust geometry. Coverage shows the selected mask in red without contaminating export or histogram output. Local exposure, contrast, white balance and saturation remain non-destructive.

**Brush**, **B**, or **Paint selected** enables freehand editing. **New brush** creates a new mask. Expand Brush settings for size, feather, flow and density. Paint adds spatial coverage; Erase subtracts it; Alt temporarily erases. Brackets adjust size. A stroke ends on release, is one undo transaction, and can be canceled with Escape. Pressure-aware pen input is supported by the code but not certified on physical hardware here. Current brush coverage is capped at a 1024-pixel long edge, including export.

Clone uses up to 32 feathered stamps: Alt-click selects a source, then click a destination. It is not healing or generative removal. Mask capacity is eight; brush storage and replay have documented safety limits. [Detailed brush semantics](ADVANCED-EDITING.md#freehand-brush-masks)

## Compare and preserve looks

Before/after uses a draggable divider. Original toggles the original source view. Fit resets pan and zoom; 100% uses source-pixel/display-scale geometry. Viewport decoding remains bounded to 2560 pixels, so larger sources do not expose native-resolution detail through zoom alone. Export decodes the retained original.

Named versions preserve complete photo states and are saved in catalogs. Restoring a version is undoable. Session undo/redo retains up to 100 transactions; imports and album structural changes are not yet undoable.

## XMP sidecars

Open Photo information or XMP in Edit. Import XMP lets you inspect supported properties and compatibility warnings before Apply. Metadata only preserves processing; the native-settings option chooses between a complete LightSpace extension and the limited Camera Raw mapping. Import affects the active photograph as one undoable edit.

Export XMP writes a separate file with rating, label, caption and keywords. Processing export is an explicit Camera Raw parameter/curve subset, plus optional complete LightSpace settings. Unsupported Adobe profiles, AI masks and processing are not equivalent. Unknown third-party properties are not retained on re-export. Original image bytes remain unchanged. [Supported fields and limits](ADVANCED-EDITING.md#xmp-interchange)

## Save and export

The footer's dedicated save indicator acknowledges completed committed revisions, not live previews. Click it to save/retry. Browser recovery uses origin-local IndexedDB; native recovery uses `LightSpace/recovery.lightspace` under application data. Both are unencrypted. Unreadable previous recovery is protected until explicit replacement. Browser unload warnings are not a guarantee against crashes or forced termination.

Save catalog exports originals, edits, metadata, albums and versions as `.lightspace`. New files use schema 3; schemas 1 and 2 migrate with neutral new settings. Older builds reject schema 3. Opening a catalog replaces the workspace: save a backup first.

Image export produces 8-bit sRGB JPEG/PNG/WebP copies up to an 8192-pixel long edge, optionally ZIP-packaging a selection. Source EXIF/IPTC is not embedded in those rendered copies. Retain source files and portable catalog backups; local recovery is not the sole archive for irreplaceable photographs.

## Keyboard

| Keys | Action |
| --- | --- |
| G / E / D | Grid / edit |
| R / M / B | Crop / masking / brush |
| Z / Y / Backslash | Fit/source-pixel zoom / comparison / original |
| 0–5 / P / X / U | Rating / pick / reject / clear flag |
| Left / Right | Previous / next photo |
| Alt while brushing | Temporary erase |
| [ / ] while brushing | Smaller / larger brush |
| Ctrl/Cmd+Z / Ctrl/Cmd+Shift+Z | Undo / redo |
| Ctrl/Cmd+I / S / E | Import / catalog backup / export |
| Ctrl/Cmd+A / C / V | Select filtered photos / copy edits / paste edits |
| Escape | Cancel the active gesture or dialog; otherwise leave the current tool |

Curve and slider controls consume their own focused editing keys. Text fields retain normal typing behavior. The in-app Help entry introduces the core workflow; this guide and Advanced editing describe the full current feature boundary.
