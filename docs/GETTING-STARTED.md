# Getting started

## Open and organize

LightSpace restores valid local recovery or opens its demonstration catalog. Add photos imports JPEG, PNG, WebP, BMP or GIF without modifying originals. RAW, DNG, HEIF and TIFF are not decoded.

Select in the grid or filmstrip; Control-click extends selection. Search matches filenames, copy names, captions and keywords. Favorites uses four stars or higher; Picks/Rejected filter flags. Albums reference records, not duplicated sources. Grid/filmstrip show sixty-photo pages. Originals and Virtual copies filters distinguish source records and alternatives.

## Virtual copies and culling

Manage virtual copies creates one alternative or copies for the selection. Each has independent processing/metadata, sharing immutable original bytes. A copy of a copy points to the original directly. Names are unique within a family; album membership is inherited, while named versions start empty. Confirmed removal affects only copy records and memberships, never original files; create/rename/remove are undoable. After removing the active copy, a surviving selected photo becomes active or its original is selected when none remains. [Family guide](VIRTUAL-COPIES.md)

Survey or N compares the selection, falling back to filtered photos when fewer than two are selected. Twelve previews appear per page. Ratings and P/X/U apply only to the active candidate. Exclude or Delete removes it from the current survey, not the catalog. Restore excluded reverses that narrowing. Enter/double-click opens Detail. Survey and exclusions are session-local. [Survey](SURVEY.md)

## Develop and transfer

Drag a slider or enter its value; double-click resets. Release commits one gesture and Escape cancels capture. RGB curves supports master/R/G/B, up to thirty-two points each, numeric/keyboard edits and smooth or linear interpolation. Grade opens four tonal/global wheels. Presets and detail/effect controls use original LightSpace processing, not calibrated Adobe algorithms. [Advanced editing](ADVANCED-EDITING.md) · [Color/masks](COLOR-AND-MASKS.md)

Copy settings/Sync offer thirteen processing groups. Global excludes geometry, crop, masks and clones. Source and targets are captured before review; one batch transaction retains target metadata and unselected edits. Ctrl/Cmd+C copies all processing, Ctrl/Cmd+Shift+C opens group selection, and Ctrl/Cmd+V pastes. Shift+R pins a session-local frozen reference with linked or independent navigation, side-by-side/stacked layouts and selective matching. [Reference/settings](REFERENCE-AND-SYNC.md)

## Crop, ratios and composition guides

Crop or R exposes presets, custom width/height inputs, lock, orientation swap and guides. Choosing a preset or applying a custom ratio enables locking. Ratios describe final output dimensions after quarter turns; X swaps width/height without rotating or rejecting the photograph. A toggles locking. Original restores full-frame bounds; Reset crop also resets geometry while retaining global development and optics.

Locked corner/edge drags retain the current ratio and opposite anchor. Shift temporarily locks an unlocked crop for one gesture. Alt/Option resizes around the center. Drag the interior to move; drag outside the crop to create a new rectangle. Policies are captured at pointer-down. Bounds remain inside the image with the existing approximately one-percent minimum extent. Arrow keys nudge one source pixel, or ten with Shift. Enter outside text inputs returns to Edit; Escape cancels a captured edit.

O cycles Thirds, Grid, Golden ratio, Diagonals, Triangle and None; Shift+O reverses Triangle. Inspector controls expose the same operations. Guides affect neither image processing nor exports. Lock/guide/custom-input state is session-local; the resulting numeric crop persists in catalogs, copies and recovery.

Resize, display-scale or viewport-position changes invalidate a captured coordinate frame and cancel the uncommitted crop rather than reinterpret old coordinates. After layout settles, begin another drag. [Crop math, API and regression evidence](CROP-CONSTRAINTS.md)

## Optics, geometry and straightening

Optics supplies manual distortion, lens falloff and radial red/cyan or blue/yellow alignment. Geometry provides perspective, Rotate, Aspect, Scale and offsets. Constrain crop conservatively enlarges the corrected image to fill its frame; it is not a maximum-area solver or automatic/guided Upright. Draw a horizon with Straighten to correct its angle; Escape restores the opening value. Source-coordinate masks/picking remain attached through corrections. [Geometry](OPTICS-GEOMETRY.md)

White balance or W samples a neutral source-preview patch for relative Temperature/Tint. Black/transparent samples are rejected; this is not RAW Kelvin calibration. Histogram regions adjust Blacks, Shadows, Exposure, Highlights or Whites. Corner triangles or J toggle display-only clipping, excluded from exports and histogram measurements.

## Masks and comparison

Drag radial/linear gradients and their handles. Luminance restricts source brightness. Color range uses click to replace, Shift-click to add up to five samples and Alt-click or swatches to remove. Coverage shows selection in red without affecting exports.

New brush creates a mask; Paint selected edits existing spatial coverage. Paint adds, Erase subtracts and Alt temporarily erases. Size, feather, flow, density and supplied pressure are captured per stroke. Brackets change size; Escape cancels. Brush coverage remains limited to a 1024-pixel long edge, including export. Physical pen hardware is not certified by CI. Clone supports thirty-two feathered source stamps, not healing or generative removal.

Before/after retains optical/geometric alignment and has a draggable divider. Fit resets navigation; 100% uses source-pixel geometry with display scale. Editing previews target 2560 pixels; zoom does not restore omitted native-resolution detail. Resize side panels with grips. F6 toggles focus layout; F7 toggles the filmstrip. These are session-local view changes.

## XMP, export and recovery

Photo information offers reviewed XMP import/export. Metadata only leaves processing intact; the native extension round-trips the selected look's processing, not family relationships. Standard Camera Raw fields are a reported subset, not equivalent Adobe development. Unknown properties are not retained on re-export.

Image export supports JPEG/PNG/WebP, quality/size and optional selection ZIP. Copy filenames include their name/identity. Output is 8-bit sRGB, at most 8192 pixels on the long edge, without source EXIF/IPTC embedding. Geometry/optics are included; overlays/guides are excluded.

Save catalog exports originals, families, edits, albums, metadata and versions. Source bytes are stored once per family and relationships validated on load. Open catalog replaces the workspace; back it up first. Catalog schema 6 migrates 1–5; native XMP 5 accepts 3–4; recovery manifest 1 and IndexedDB 2 are unchanged. Older builds reject unsupported schemas. Keep pre-upgrade backups and reload without clearing site data.

The footer acknowledges completed committed revisions, never previews. Click to flush/retry. Recovery uses SHA-256 originals and separate metadata. Missing/corrupt data stays protected until explicit replacement. Stores are local, unencrypted and do not merge tabs or clean orphan sources. Keep original files and portable backups. [Recovery contract](RECOVERY.md)

## Keyboard reference

| Keys | Action |
| --- | --- |
| G / E or D | Grid / Edit |
| N / Shift+R | Survey / reference |
| R / M / B | Crop / Masking / Brush |
| A / X in Crop | Lock / swap width and height |
| O / Shift+O in Crop | Guide / reverse triangle |
| Arrow / Shift+Arrow in Crop | Nudge by one / ten source pixels |
| Enter in Crop | Apply custom input when focused there; otherwise return to Edit |
| W / J | White balance / clipping |
| F6 / F7 | Focus layout / filmstrip |
| [ / ] | Brush size |
| Z / Y / Backslash | Zoom / before-after / original |
| 0–5 / P, X, U | Rating / pick, reject, clear outside crop-specific shortcuts |
| Enter / Delete in Survey | Detail / exclude without deletion |
| Ctrl/Cmd+Z / Ctrl/Cmd+Shift+Z | Undo / redo |
| Ctrl/Cmd+I / S / E | Import / catalog backup / export |
| Ctrl/Cmd+A / C / V | Select filtered / copy all processing / paste captured groups |
| Ctrl/Cmd+Shift+C | Select groups to copy |
| Escape | Cancel active gesture or dialog |

Text fields retain ordinary typing and selection. Focused controls consume their own editing shortcuts. The in-app Help includes the same behavior and limitations.
