# Feature coverage and boundaries

## 0.2.0-alpha.1

LightSpace is a functional independent Uno photography workspace. It does not claim exact visual reproduction or Adobe processing/catalog compatibility. The current implementation and its boundaries are listed below.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Shell | Dark library/sidebar, centered photo canvas, filmstrip, histogram, inspector, tool rail, original iconography and custom controls | Not pixel-identical Lightroom; no Lightroom Classic modules; some standard Uno input primitives remain |
| Reuse | Eight packable libraries; independent grading wheel/editor, mixer, mask editor, cards, slider, curves, histogram and viewport | Not every workbench section is a standalone public control |
| Import | JPEG, PNG, WebP, BMP, first GIF frame; EXIF orientation and sRGB conversion; original bytes retained | No RAW/demosaic, HEIF, TIFF or video workflow |
| Catalog | Albums, multi-selection, text/keyword search, ratings, flags, 60-photo pages | No indexed large-catalog database, folder watching, relinking or Lightroom catalog import |
| Light and color | Tonal sliders, relative white balance, vibrance/saturation, monochrome, presets | Original approximations; no calibrated camera profiles or processing parity |
| Curves and mixer | Five-point RGB curve, eight-band HSV-based mixer | No arbitrary/per-channel curves or perceptual point-color selection |
| Color grading | Shadows/midtones/highlights/global wheels, hue/saturation/luminance, blending, balance, range reset; undo/copy/sync/recovery | Original LDR grading model, not Adobe pixels or scene-referred RAW/HDR grading |
| Crop | Free crop, handles, movement, common centered ratios, quarter turns and flips | No free-angle straightening, perspective correction or continuously locked aspect while dragging |
| Local masks | Rotatable radial/linear gradients with live move/resize/rotation handles; standalone luminance ranges; spatial/range intersection; local exposure/contrast/WB/saturation; opacity, disable, inversion, rename/duplicate/delete | Eight-mask limit; no brush/color-range/AI selection or general boolean-composition graph |
| Mask inspection | Outlines and selected-mask weighted red coverage; no overlay in export or histogram | No generalized mask raster editor |
| Clone | Up to 32 feathered source stamps, Alt-click source | Not healing, content-aware or generative removal |
| Detail/effects | Sharpening/spatial smoothing, texture/clarity/dehaze approximations, vignette/grain | No AI denoise, super-resolution or calibrated lens correction; limited preview spatial information |
| Navigation | Fit/source-pixel zoom geometry, wheel anchor, bounded pan, photo keyboard navigation | Preview capped at 2560-pixel long edge; no native-resolution tiled inspection/minimap |
| Comparison | Original toggle and draggable before/after split without a document edit | No second-reference-photo, side-by-side/survey workflow |
| Undo and versions | One-gesture transactions, batch edits, 100-step undo and persistent named snapshots | Undo session-only; imports/album structural changes not undoable |
| Metadata/export | Captions/keywords/ratings/flags; JPEG/PNG/WebP and batch ZIP; portable catalog | Rendered exports 8-bit sRGB, cap8192, source EXIF/IPTC not copied; no print/soft proof/tethering |
| Recovery | Committed-revision single writer, retry, protected unreadable recovery, browser unsaved-change warnings | Single JSON record; no cross-tab merge, cloud sync, encryption or guaranteed close-time flush |
| Performance | Pixel-aware caches, neutral processing bypasses, bounded thumbnail decode, stable page controls, direct state equality, bounded auto-tone sampling | Synchronous CPU decode/export; not a separate WebGPU compute graph or hardware performance certification |
| Interchange | Schema-1 import migration and schema-2 saves preserving new processing parameters | Older builds reject schema2; no Adobe presets/profiles, XMP or .lrcat compatibility |
| Delivery | Engine/pixel/recovery tests, pointer-driven browser tests, desktop build matrix, gated Pages publishing, NuGet package artifacts | No NuGet.org publication, signed installers, notarization or auto-update service |

See [color and mask workflows](COLOR-AND-MASKS.md), [performance evidence and limits](PERFORMANCE.md), [architecture](ARCHITECTURE.md) and [recovery invariants](RECOVERY.md). Remaining features are not represented as completed merely because an extension point exists.
