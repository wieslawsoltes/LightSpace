# Getting started

## Import and organize

Open the browser application or desktop host. A saved recovery catalog is restored when available; otherwise a demonstration catalog opens. Add photos imports JPEG, PNG, WebP, BMP or GIF without replacing original bytes. RAW/HEIF/TIFF are not supported.

Use the grid or filmstrip to select a photo; Control-click extends selection. Search matches filenames, captions and keywords. Favorites selects ratings4–5; Picks/Rejected filter flags. Albums reference photo IDs rather than duplicating sources. The previous/next page controls browse groups of60 photographs.

## Develop light and color

Edit contains Light, Point curve, Color, Color mixer, Color grading, Effects and Detail sections. Drag a slider, type a value, or use arrow keys when focused. A completed gesture is one undo transaction; Escape cancels a live slider gesture; double-click resets it. Section expansion is retained during the session.

Temperature/tint are relative adjustments, not calibrated camera Kelvin values. Auto is a luminance heuristic. The five-point curve is piecewise linear RGB; the eight-band mixer is an HSV approximation. Detail smoothing is not AI denoising.

Grade opens the dedicated four-range grading panel. Select Shadows, Mids, Highs or Global, use the wheel/numeric hue and saturation, then refine luminance, blending and balance. Grading can tint monochrome photographs. See the [color and mask guide](COLOR-AND-MASKS.md) for processing semantics and controls.

Copy/Paste and Sync copy processing settings while retaining target metadata. Presets replace global development while retaining crop/masks. Reset edits clears development, crop, local masks and clone spots but leaves metadata intact.

## Crop, mask and clone

Crop (R) offers a free rectangle, draggable handles, movement, common centered aspect ratios, quarter turns and flips. A click without a meaningful drag does not replace the crop. The source is shown unrotated in crop mode; Done displays the transformed result. Free-angle straightening and perspective corrections are not implemented.

Masking provides Radial, Linear and Luminance creation. Drag gradients on the image; move their pins, adjust radius/fade handles and rotate. Shift-drag creates another mask over an existing pin. Luminance selection can stand alone or restrict a spatial mask. Enable Coverage to inspect the weighted selection; it is not exported. Masks include amount, enable/disable, inversion, local tone/WB/saturation, rename/duplicate/delete and undo. Eight masks are supported.

Clone uses Alt-click to set a source and click to place a feathered stamp. Up to32 stamps are supported. This is ordinary cloning, not healing or generative removal.

## View, compare and versions

Fit resets zoom/pan.100% maps source-pixel geometry to display pixels using the host rasterization scale. The preview remains capped at2560 pixels, so larger images do not gain native detail at that zoom. Wheel zoom follows the pointer; pan is bounded to keep the image reachable.

Before/after comparison has a draggable divider. Original toggles the original view at the same crop. These are viewport operations, not document transactions.

Named versions preserve complete photo-state snapshots, including metadata, in catalog backups. Restoring a version is undoable. Undo history itself is session-only and bounded to100 transactions.

## Save and export

The footer separately reports recovery state and operations. Saved locally means the committed revision has completed its storage write; active previews remain unsaved. Click the save control to flush or retry. Unreadable prior recovery is protected until replacement is explicitly confirmed. Browser termination can bypass prompts; portable backups remain important.

Save catalog downloads `.lightspace` JSON with originals, edits, albums, metadata and named versions. Open catalog replaces the active in-memory workspace; save a backup first. Schema1 catalogs migrate on import and new saves use schema2. Older builds reject schema2, so retain a pre-upgrade backup for older-version use.

Export creates JPEG/PNG/WebP copies with quality/long-edge settings and optional selected-photo ZIP. Output is8-bit sRGB, max8192 pixels on the long edge, without source EXIF/IPTC metadata. The original bytes remain intact in catalog backups. There is no application cloud account, telemetry or photo upload.

## Keyboard reference

| Keys | Action |
| --- | --- |
| G | Grid |
| E / D | Edit/detail |
| R | Crop |
| M | Masking |
| Z | Fit/source-pixel zoom geometry |
| Y | Before/after split |
| Backslash | Original toggle |
|0–5 | Rating |
| P / X / U | Pick / reject / clear flag |
| Left / right | Previous / next photo |
| Ctrl/Cmd+Z | Undo |
| Ctrl/Cmd+Shift+Z or Ctrl+Y | Redo |
| Ctrl/Cmd+I | Import |
| Ctrl/Cmd+S | Catalog backup |
| Ctrl/Cmd+E | Export |
| Ctrl/Cmd+A | Select filtered photographs |
| Ctrl/Cmd+C / V | Copy / paste edit settings |
| Escape | Cancel active gesture/dialog |

Text fields retain typing behavior. Within the grading wheel, left/right adjusts hue, up/down adjusts saturation, Home resets color, and Escape cancels the active gesture.
