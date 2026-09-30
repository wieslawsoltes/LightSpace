# Getting started

## Open and organize

LightSpace restores valid local recovery or opens its demonstration catalog. Add photos imports JPEG, PNG, WebP, BMP or GIF sources without changing their original bytes. Camera RAW, DNG, HEIF and TIFF are not decoded.

Select a photograph in the grid or filmstrip; Control-click extends selection. Search matches filename, copy name, caption and keyword tokens. Favorites uses four stars or higher; Picks/Rejected filter flags. Albums reference photos instead of duplicating originals. Grid and filmstrip show pages of up to 60 photos. Originals and Virtual copies filters distinguish source records from alternative looks.

## Virtual copies

Open **Manage virtual copies** on the right tool rail. Create virtual copy captures the active photograph's final look; Create for selection makes a copy for each selected photograph in one transaction. Each copy has independent processing and metadata while sharing the original source bytes. Creating from another copy captures that look but references the original directly.

The family panel selects and renames alternatives, shows their original, and offers **Survey this original**. Copy names must be unique within the family. Copies inherit source album memberships; later edits remain independent. Named versions start empty on the new copy rather than being duplicated from its source.

**Remove this copy…** requires confirmation and removes only the virtual-copy record and its album memberships. It does not delete an original catalog record, sibling copy or source file. Undo restores the same copy identity and appearance. Creation, rename and removal are undoable. A virtual copy persists in catalogs/recovery, unlike a session-local reference view. [Copy-family behavior and APIs](VIRTUAL-COPIES.md)

## Survey and reference comparison

Survey view or **N** compares the selected photographs; with fewer than two selected, it uses the filtered sequence and reports that choice. Up to twelve aspect-aware previews appear per page. Click a candidate, then use 0–5 or P/X/U to change only its rating/flag. The cross or Delete/Backspace excludes it from the survey, never from the catalog. Hide rejected and Restore excluded refine the group. Enter or double-click opens the candidate in Detail. Exclusions are session-local. [Survey guide](SURVEY.md)

Reference view or **Shift+R** pins the current look beside active editing. Choose another filmstrip photograph or keep editing while the reference stays frozen. Pin again, fit both, switch side-by-side/stacked layouts, and link or separate navigation using the header. Linked pan/zoom is fit-relative, not automatic image registration. The reference clears when closed, the catalog changes, or the application reloads. [Reference workflow](REFERENCE-AND-SYNC.md)

## Develop and transfer settings

Edit supplies Light, Color, curves, mixer, grading, effects, detail, Optics and Geometry sections. Drag a slider or type its value. Double-click resets its default, release commits one gesture, and Escape cancels a captured gesture.

RGB curves supports master/red/green/blue curves with up to 32 points, numeric values, keyboard editing and smooth or linear interpolation. Grade opens four tonal/global wheels. Brush/range masks, clone stamps and named versions retain non-destructive state. These are original LightSpace algorithms, not Adobe processing equivalence. [Advanced editing](ADVANCED-EDITING.md) · [Color and masks](COLOR-AND-MASKS.md)

Copy settings and Synchronize settings offer thirteen selectable processing groups. Global excludes geometry, crop, masks and clone spots. Copy captures the checked groups; Paste applies those groups to the current selection. Synchronize captures the source and other selected targets before review and commits one batch transaction. Target metadata and unselected processing stay intact. Ctrl/Cmd+C copies all processing immediately; Ctrl/Cmd+Shift+C opens group selection; Ctrl/Cmd+V pastes.

## Optics, geometry and crop

Use Optics for manual distortion, lens falloff and red/cyan or blue/yellow radial alignment. These are original manual corrections, not an automatic lens-profile database. Geometry supplies vertical/horizontal perspective, Rotate, Aspect, Scale and X/Y offsets. Constrain crop conservatively enlarges the transformed image to fill the frame; it is not Adobe automatic/guided Upright or a maximum-area optimizer.

Crop has handles, movement, centered ratio presets, quarter turns and flips. Straighten activates a horizon-line tool: drag along a horizon and release to correct its angle; Escape restores it. Done returns to Edit. Ratio presets are not continuously locked during later free dragging. Reset crop clears framing and geometry, not global development or optics.

Masks and sampled colors remain in original-source coordinates. Drawing and picking use the optical/projective mappings so positions remain attached when the image is corrected. [Math and limits](OPTICS-GEOMETRY.md)

## White balance, histogram and clipping

Press W or choose the White balance picker, then click a neutral patch. A small source-preview sample sets relative Temperature and Tint; black/transparent areas are rejected and model limits are reported. This is not camera Kelvin or RAW calibration.

Drag a histogram region—Blacks, Shadows, Exposure, Highlights or Whites—to adjust that tone. Release commits once; Escape cancels. Corner triangles toggle blue shadow/red highlight clipping independently; J toggles both. Indicators are excluded from histogram measurements, exports and catalog state. They describe LDR display thresholds, not RAW recoverability.

## Workspace and zoom

Drag library/edit-panel dividers to resize. Escape cancels, double-click/Home resets, and focused arrows make small changes. F6/Focus mode hides side panels and filmstrip; F7 toggles the filmstrip. Layout remains in the workbench session, not the catalog.

Original/Before compares development while retaining optical/geometric framing. Drag the comparison divider without editing the photo. Fit resets navigation; 100% uses source-pixel geometry and display scale. The preview targets a 2560-pixel long edge, so large photographs do not gain native-resolution detail simply by zooming.

## Masks and clone

Drag radial/linear gradients and their handles. Luminance restricts source brightness; Color range uses click to replace, Shift-click to add up to five samples, and Alt-click or a swatch to remove. Coverage shows the selected mask in red and never enters exports.

New brush creates a mask; Paint selected modifies existing spatial coverage. Paint adds, Erase subtracts, and Alt temporarily erases. Size, feather, flow, density and supplied pen pressure are captured per stroke. Brackets change size; Escape cancels. Coverage is capped at 1024 pixels even for export. Physical pen hardware is not certified by CI.

Clone uses Alt-click for a source and click for destinations, with up to 32 feathered stamps. It is not healing or generative removal.

## XMP, export and recovery

Photo information supplies Import/Export XMP. Review the applied/unsupported-field report before Apply. Metadata only leaves processing intact. The native extension round-trips the selected look's settings, not its virtual-copy family. Standard Camera Raw mapping remains a reported scalar/curve subset, not equivalent Adobe development. Unknown properties are not retained on re-export.

Export produces JPEG/PNG/WebP with quality/size choices and an optional selected-photo ZIP. Copy exports include their name and short identity in the filename. Output is 8-bit sRGB with an 8192-pixel long-edge cap. Source EXIF/IPTC is not embedded. Geometry and optics are included; guides/overlays are excluded.

Save catalog exports a portable `.lightspace` file with originals, alternatives, processing, albums, metadata and versions. Each original's bytes are stored once per family; virtual-copy records reference it. Loading validates family relationships before restoring shared sources. Open catalog replaces the workspace: save a backup first.

New catalog **schema 6** migrates versions 1–5. Native XMP settings remain **5** and accept versions 3–4. Recovery manifest **1** and IndexedDB **2** are unchanged. Older builds reject schema 6; keep pre-upgrade portable backups for older-version interoperability. **Do not clear site data to load the update.**

The footer acknowledges only completed committed recovery revisions, not previews. Click to flush/retry. SHA-256 sources are stored separately from metadata; creating or renaming a copy can reuse its existing original blob. Missing/corrupt recovery stays protected until explicit replacement. Native recovery uses the application-data LightSpace directory. Stores are local, unencrypted, not cross-tab-merged and not automatically compacted. Keep original files and portable backups. [Recovery contract](RECOVERY.md)

## Keyboard reference

| Keys | Action |
| --- | --- |
| G / E or D | Grid / Edit |
| N / Shift+R | Survey / frozen reference |
| R / M / B | Crop / Masking / Brush |
| W / J | White-balance picker / clipping indicators |
| F6 / F7 | Focus workspace / filmstrip |
| [ / ] | Brush size |
| Z / Y / Backslash | Zoom / before-after / original |
| 0–5 / P, X, U | Rating / pick, reject, clear flag |
| Left / right | Photo or Survey navigation unless consumed by a focused editor |
| Enter / Delete in Survey | Open Detail / exclude candidate without deleting its catalog record |
| Ctrl/Cmd+Z / Ctrl/Cmd+Shift+Z | Undo / redo |
| Ctrl/Cmd+I / S / E | Import photos / catalog backup / export |
| Ctrl/Cmd+A / C / V | Select filtered / copy all processing / paste captured settings |
| Ctrl/Cmd+Shift+C | Select processing groups to copy |
| Escape | Cancel active gesture or dialog |

Text fields retain typing behavior. Focused editors and resize grips have their own keyboard actions. The in-app Help panel documents the same workflows and boundaries.
