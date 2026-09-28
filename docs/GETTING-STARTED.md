# Getting started

## Open and organize

The app opens the last valid recovery or a demonstration catalog. Add photos imports JPEG, PNG, WebP, BMP or the first GIF frame while retaining original bytes. RAW, DNG, HEIF and TIFF are not decoded.

Browse the grid, detail view or filmstrip; Control-click extends selection. Search matches filenames, captions and keywords. Favorites selects ratings of four or more; Picks/Rejected select flags. Albums reference photo IDs. Grid/filmstrip browsing uses pages of 60 photographs.

## Develop

Edit contains Light, Color, legacy Point curve, RGB curves, Color mixer, Color grading, Effects and Detail. Drag sliders, type numeric values or use focused arrow keys. Double-click resets a slider. A release commits one gesture; Escape cancels it.

Temperature/tint, Auto tone, HSV mixing, detail and grading are original LightSpace processing, not calibrated camera white balance or Adobe/AI algorithms. Grade opens four tonal-range wheels. RGB curves opens master and per-channel point editing with linear/smooth interpolation, up to 32 points per channel. Click to add, drag to reshape, edit Input/Output or delete an interior point.

Copy/paste and Sync apply processing settings to selected photos while preserving target metadata. Reset edits clears processing, crop, masks and clone spots. Named versions preserve complete alternative states. See [advanced editing](ADVANCED-EDITING.md).

## Crop, masks and source selection

Crop supports free rectangles, handles, movement, centered ratios, quarter turns and flips. It does not provide arbitrary straightening or perspective correction. Done returns to editing.

Masking provides rotatable radial/linear gradients, luminance ranges, sampled color ranges and brushes. Drag gradients and edit their pins/fade/radius/rotation handles. Luminance selects source sRGB brightness. Color range selects up to five averaged source-preview colors: click replaces, Shift-click adds and Alt-click removes a pin. Swatches also remove samples. Sample on an existing mask restricts its geometry rather than creating another mask. Tolerance/smoothness use Oklab distances, not Adobe units or calibrated RAW color.

New brush creates a brush mask; Paint selected modifies current coverage. Paint adds, Erase subtracts, and Alt temporarily erases. B selects the brush; brackets change size. Expand Brush settings for size, feather, flow and density. Pressure is consumed when supplied. Each stroke supports undo/cancellation. Coverage is rasterized to a maximum 1024-pixel long edge even for export; native-resolution brush quality and physical pen hardware are not certified.

Up to eight masks support enable/disable, amount, inversion, local tone/color, rename, duplicate and delete. Coverage displays a red selection aid that is excluded from exported images and histograms. Restrictions intersect with spatial/brush coverage before inversion and amount. [Mask semantics and limits](COLOR-AND-MASKS.md)

Clone is separate: Alt-click a source, then click a destination. It supports 32 feathered stamps, not healing or generative removal.

## Compare and inspect

Original toggles the original view. Before/after splits the image; drag its divider without creating an edit. Fit resets pan/zoom. 100% accounts for source-pixel geometry and display scale. The preview is bounded to a 2560-pixel long edge, so zoom cannot reveal detail missing from a larger source's preview. Wheel zoom follows the pointer.

## XMP and export

Photo information / XMP offers sidecar import/export. Review supported fields and warnings before Apply. Metadata only leaves processing unchanged. The native extension can retain all LightSpace settings; the ordinary Camera Raw mapping is a documented parameter/curve subset. Unsupported processing is reported; unknown external fields are not retained on re-export. Original image bytes are never modified by XMP operations.

Export produces 8-bit sRGB JPEG, PNG or WebP with quality/size choices and an optional selected-photo ZIP. Maximum long edge is 8192 pixels. Source EXIF/IPTC is not embedded in rendered copies; original bytes in catalog backups remain intact.

## Recovery and compatibility

The footer save control reports the last completed committed revision, independently of transient import/export messages. Click it to flush/retry. A live preview remains unsaved even if the preceding committed revision has been saved. Browser navigation warns about pending work but termination can bypass that warning.

Modern recovery separates originals under SHA-256 keys from the edit manifest. Warm saves do not rewrite or rehash unchanged originals. Browser storage is IndexedDB; native storage is `LightSpace/recovery-manifest-v1.json` plus `originals-v1` under application data. Missing/corrupt data is protected until explicit Replace recovery. Replacement saves the current workspace; it does not repair the previous one.

Save catalog exports a portable `.lightspace` file containing originals, edits, albums, metadata and versions. Open catalog replaces the active workspace; save a backup first. New files use schema **4** and migrate schemas 1–3. Native XMP extension 4 accepts version 3. Browser database version 2 is incompatible with older applications requesting version 1. Keep pre-upgrade portable backups for old-version interoperability.

Legacy source-inclusive recovery remains readable when no modern manifest exists, and migrates at the next committed save. Native `recovery.lightspace` is a legacy fallback, not the current manifest. Recovery is local, unencrypted and not cross-tab-merged. Unreferenced originals remain retained; there is no automatic compaction or crash journal. Keep source files and portable backups. [Recovery contract](RECOVERY.md) · [Publication safety](RECOVERY-PUBLICATION.md)

## Keyboard reference

| Keys | Action |
| --- | --- |
| G; E/D | Grid; Edit/detail |
| R; M; B | Crop; Masks; Brush |
| [ / ] | Brush size when active |
| Z; Y; Backslash | Zoom; comparison; original |
| 0–5; P/X/U | Rating; pick/reject/clear flag |
| Left/right | Previous/next photo unless a focused editor consumes them |
| Ctrl/Cmd+Z; Ctrl/Cmd+Shift+Z | Undo; redo |
| Ctrl/Cmd+I; Ctrl/Cmd+S; Ctrl/Cmd+E | Import; catalog backup; export |
| Ctrl/Cmd+A; Ctrl/Cmd+C/V | Select filtered; copy/paste settings |
| Escape | Cancel active gesture or dialog |

Text fields preserve normal typing. Focused slider/curve/wheel controls implement their own editing keys. In-app Help documents these workflows and their limits.
