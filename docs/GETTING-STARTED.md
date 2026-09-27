# Getting started

## Open a workspace

Launch the browser application or desktop host. LightSpace restores the last successfully written recovery catalog when available. Otherwise it opens a demonstration catalog. No account or cloud storage is required.

Use **Add photos** to import one or more JPEG, PNG, WebP, BMP, or GIF files. The originals are retained in memory and in catalog backups. RAW/HEIF/TIFF files are not supported in this release. Unsupported inputs report an error instead of silently substituting a preview.

## Browse and select

Use the grid or filmstrip to select a photo. Control-click extends the selection. The search field matches tokens across filenames, captions, and keywords. Favorites includes photographs rated four stars or higher; Picks and Rejected filter the corresponding flag. Albums store photo IDs, not duplicate source files. Creating an album adds the current selection.

The grid and filmstrip show one page of up to 60 photographs; use the breadcrumb's previous/next controls for additional pages. Arrow keys navigate the filtered photo sequence.

## Develop

The Edit panel contains a histogram and collapsible Light, Point curve, Color, Color mixer, Effects, and Detail sections. Drag a slider, use arrow keys when it is focused, or edit its numeric value. A completed pointer gesture is a single undo transaction. Double-click a slider to reset its default value.

Temperature and tint are relative channel adjustments, not calibrated camera white-balance temperatures. Auto is a deterministic luminance-based heuristic. Creative presets replace the global development settings but retain the crop and local masks. The five-point curve is piecewise linear. The eight-band mixer uses a hue-weighted HSV approximation.

Copy/Paste and Sync operate on edit settings; syncing retains each target's rating, flags, caption, and keywords. Reset edits clears global adjustments, crop, local masks, and clone spots but retains catalog metadata.

## Crop, masks and clone

Choose Crop from the tool rail or press R. Drag a new crop rectangle, resize an edge/corner, or move an existing crop. A click without a meaningful drag does not replace the crop. Aspect buttons create centered crops. Quarter-turn rotations and flips are applied after cropping. In crop mode the unrotated source is shown to make source coordinates explicit; Done displays the transformed result.

Choose Masking, select Radial or Linear, and drag on the photograph. The current release supports up to eight radial or vertical linear gradients. Select a mask in the panel to change exposure, saturation, feathering and position, invert it, or remove it. The outline toggle controls the editing guide, not the mask's effect.

The Clone tool supports up to 32 feathered source stamps. Alt-click chooses a source; a normal click stamps its pixels at a destination. This is ordinary cloning, not healing, object selection, or generative removal.

## Compare and preserve looks

The comparison control splits the image into original and edited halves. Original toggles the complete original view while preserving crop placement. Fit resets zoom and pan. **100%** toggles between fit and one source pixel per display pixel, accounting for the host's rasterization scale. Wheel zoom is anchored to the pointer, and panning retains a reachable portion of the photograph.

Viewport decoding is currently limited to a 2560-pixel long edge. Consequently, source-pixel zoom provides correct geometry but cannot reveal full native-resolution detail for larger photographs; tiled/full-resolution inspection remains an extension point. Export decodes the retained original rather than exporting the preview.

Versions preserve complete photo-state snapshots, including catalog metadata. Restoring a version creates a new undoable transaction. Undo/redo history is bounded to 100 transactions and lives in the current session; named versions are included in catalog backups.

## Save and export

The footer reports when recovery has been saved. Do not treat an edit still awaiting recovery as durable. Browser recovery uses IndexedDB on the current origin and browser profile. Native recovery is stored under the platform's local application-data directory in `LightSpace/recovery.lightspace` using a temporary-file replacement.

**Save catalog** downloads a `.lightspace` JSON container with source bytes, edits, albums, ratings, keywords, and named versions. This is the portable backup format. **Open catalog** replaces the active in-memory workspace; save a backup of the current workspace first.

**Export** produces JPEG, PNG or WebP. Choose quality and a long-edge limit. Multi-photo selections can be exported as a ZIP. Rendered exports are 8-bit sRGB and do not preserve source EXIF/IPTC metadata; originals in catalog backups remain intact. The largest supported export long edge is 8192 pixels.

## Keyboard reference

| Keys | Action |
| --- | --- |
| G | Grid |
| E / D | Edit/detail |
| R | Crop |
| M | Masking |
| Z | Toggle fit / source-pixel zoom |
| Y | Before/after split |
| Backslash | Toggle original |
| 0–5 | Rating |
| P / X / U | Pick / reject / clear flag |
| Left / right | Previous / next photo |
| Ctrl/Cmd+Z | Undo |
| Ctrl/Cmd+Shift+Z or Ctrl+Y | Redo |
| Ctrl/Cmd+I | Import photographs |
| Ctrl/Cmd+S | Save catalog backup |
| Ctrl/Cmd+E | Export |
| Ctrl/Cmd+A | Select filtered photos |
| Ctrl/Cmd+C / V | Copy / paste development settings |
| Escape | Cancel the active gesture or dialog |

Text fields retain normal typing behavior. The in-app Help button opens a built-in guide with the same workflow boundaries.
