# Getting started

## Open and organize

LightSpace restores valid local recovery or opens its demonstration catalog. Add photos imports JPEG, PNG, WebP, BMP or GIF sources without changing their original bytes. Camera RAW, DNG, HEIF and TIFF are not decoded.

Select a photograph in the grid or filmstrip; Control-click extends selection. Search matches filename, caption and keyword tokens. Favorites uses four stars or higher; Picks/Rejected filter flags. Albums reference photos instead of duplicating originals. Grid and filmstrip show pages of up to 60 photos.

## Develop, grade and refine

Edit supplies Light, Color, curves, mixer, grading, effects, detail, Optics and Geometry sections. Drag a slider or type its value. Double-click resets its default, release commits one gesture, and Escape cancels a captured gesture. Copy/Paste and Sync apply processing to selected photos while retaining their metadata.

RGB curves supports master/red/green/blue curves with up to 32 points, numeric values, keyboard editing and smooth or linear interpolation. Grade opens the four tonal/global wheels. Brush and range masks, clone stamps, and versions retain non-destructive state. [Advanced editing](ADVANCED-EDITING.md) · [Color and masks](COLOR-AND-MASKS.md)

## Optics, geometry and crop

Use the tool rail's Optics panel for manual distortion, lens falloff and red/cyan or blue/yellow radial alignment. These are original manual corrections, not an automatic lens-profile database.

Geometry provides vertical/horizontal perspective, Rotate, Aspect, Scale and X/Y offsets. Constrain crop conservatively enlarges the transformed image to fill the frame. It is not Adobe automatic/guided Upright or a maximum-area crop optimizer.

Crop retains handles, movement, centered ratio presets, quarter turns and flips. Straighten activates a horizon-line tool: drag along a horizon and release to correct the angle, or Escape to restore it. Done returns to Edit. Ratio presets are not continuously locked during later free dragging. Reset crop clears its framing and geometry, not global development or optics.

Masks and sampled colors remain in original-source coordinates. Drawing and picking use the forward/inverse optical and projective mappings, so these positions remain attached when the image is corrected. [Math, semantics and limits](OPTICS-GEOMETRY.md)

## White balance, histogram and clipping

Press W or choose the White balance picker, then click a neutral patch. A small source-preview sample sets relative Temperature and Tint; black/transparent areas are rejected and model limits are reported. It is not camera Kelvin or RAW white-balance calibration.

Drag one of the histogram's five regions—Blacks, Shadows, Exposure, Highlights or Whites—to adjust that tone. A drag is one undoable transaction; Escape cancels. Corner triangles toggle blue shadow/red highlight clipping independently; J toggles both. Indicators are view-only, excluded from histogram measurements, exported images and catalog state. They describe LDR display thresholds, not RAW recoverability.

## Workspace, comparison and zoom

Drag library/edit-panel dividers to resize. Escape cancels, double-click/Home resets, and focused arrow keys make small width changes. F6/Focus mode hides side panels and filmstrip; F7 toggles the filmstrip. Widths and visibility stay in the running workbench session, not the catalog.

Original/Before compares development while retaining optical/geometric framing. Drag the comparison divider without editing the photo. Fit resets pan/zoom; 100% uses source-pixel geometry and display scale. The preview has a 2560-pixel long-edge target, so large photos do not gain native-resolution detail simply by zooming.

## Masks and clone

Drag radial/linear gradients and edit their handles. Luminance restricts source brightness; sampled Color range uses click to replace, Shift-click to add up to five colors, and Alt-click or a swatch to remove. Coverage shows the selected mask in red and never enters exports.

New brush creates a mask; Paint selected modifies its spatial coverage. Paint adds and Erase subtracts; Alt temporarily erases. Size, feather, flow, density and supplied pen pressure are captured per stroke. Brackets adjust brush size, and Escape cancels a stroke. Coverage is capped at 1024 pixels even for export. Physical pen hardware is not certified by CI.

Clone uses Alt-click to choose a source and click to stamp destinations, with up to 32 feathered stamps. It is not healing or generative removal.

## XMP, export and recovery

Photo information supplies Import/Export XMP. Review the applied/unsupported-field report before Apply. Metadata only leaves current processing intact. The native extension round-trips all LightSpace settings; standard Camera Raw mapping remains a reported scalar/curve subset, not equivalent Adobe processing. Unknown third-party properties are not retained on re-export.

Export writes JPEG, PNG or WebP with quality/size choices and optional selected-photo ZIP. Output is 8-bit sRGB, capped at an 8192-pixel long edge. Source EXIF/IPTC is not embedded; catalog originals remain intact. Geometry and optics are included, while clipping/coverage guides are not.

Save catalog exports a portable `.lightspace` file with originals, processing, albums, metadata and versions. Open catalog replaces the current workspace; save a backup first. New schema **5** migrates schemas 1–4 with neutral missing fields. Native XMP settings version 5 accepts 3–4. Older builds reject new settings, so keep pre-upgrade backups when using older versions.

The footer acknowledges only completed committed recovery revisions, not live previews. Click to flush/retry. Recovery separates SHA-256-addressed originals from its manifest and avoids rewriting unchanged sources. Missing or corrupt recovery stays protected until explicit replacement. The browser database remains version 2; no site-data reset is needed for 0.5. Native recovery uses the application-data `LightSpace` directory. Stores are local, unencrypted, not cross-tab-merged and not automatically compacted. Keep source files and portable backups.

## Keyboard reference

| Keys | Action |
| --- | --- |
| G / E or D | Grid / Edit |
| R / M / B | Crop / Masking / Brush |
| W / J | White-balance picker / clipping indicators |
| F6 / F7 | Focus workspace / filmstrip |
| [ / ] | Brush size |
| Z / Y / Backslash | Zoom / before-after / original |
| 0–5 / P, X, U | Rating / pick, reject, clear flag |
| Left / right | Photo navigation unless consumed by a focused editor |
| Ctrl/Cmd+Z / Ctrl/Cmd+Shift+Z | Undo / redo |
| Ctrl/Cmd+I / S / E | Import photos / catalog backup / export |
| Ctrl/Cmd+A / C / V | Select filtered / copy settings / paste settings |
| Escape | Cancel active gesture or dialog |

Text fields retain typing behavior. Focused sliders, curves, wheels, histogram and resize grips have their own keyboard actions. The in-app Help panel also documents these workflows and limitations.
