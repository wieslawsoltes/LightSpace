# Feature coverage and compatibility boundaries

**0.3.0-alpha.1** is an independent, functional Uno photography workspace. This ledger is a scope statement, not a claim of exact Lightroom UI, processing, file-format or feature parity.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Shell | Custom dark photography layout, navigation, albums, filmstrip, inspector, tool rail, icons and application dialogs | Not pixel-identical Adobe UI; no Lightroom Classic module suite; narrow-screen usability is limited |
| Reuse | Eight packable libraries; independent engine; reusable sliders, cards, grading/mixer/mask/brush/curve controls | Some workspace composition remains in StudioView partials; standard Uno input/scroll/select primitives remain |
| Input | JPEG, PNG, WebP, BMP and first GIF frame; EXIF orientation normalization and sRGB decode | No camera RAW/demosaic, DNG, HEIF/HEIC, TIFF, video or animated workflows |
| Catalog | Original bytes, albums, selection, ratings/flags, captions/keywords, search/filtering, 60-photo pages | Not an indexed catalog database; no folder watching, relinking, stacks, face/location indexing or Lightroom lrcat support |
| Global development | Tone, relative white balance, saturation/vibrance, monochrome, effects/detail approximations | Original LDR processing, not calibrated Adobe RAW algorithms; no profile authoring, camera/lens calibration or eyedropper |
| Curves | Legacy five-point curve plus arbitrary-point master/R/G/B, linear or shape-preserving smooth interpolation, numeric/pointer/keyboard editing | Maximum 32 points per channel; 2048-entry sampled render lookup; no claim of Adobe interpolation equivalence |
| Color grading | Shadows/midtones/highlights/global wheels, luminance, blending, balance, presets and undo | Original tonal weighting and tint model, not Adobe pixel equivalence |
| Color mixer | Eight weighted HSV bands with hue/saturation/luminance | No point-color selection or perceptual color-space parity |
| Crop/navigation | Rectangle and handles, centered ratios, quarter turns/flips, fit/source-pixel geometry, pan, wheel zoom | No arbitrary straightening angle, perspective/Upright or continuous aspect locking; previews remain bounded rather than native-resolution tiled |
| Gradient/range masks | Rotatable radial and linear masks, handles, luminance range/intersection, coverage display, amount, enable/invert, local tone/color | No color-range masks, AI selections or arbitrary mask-group boolean graph |
| Brush masks | Add/erase, pressure-aware radius/flow, feather/density, arc-length sampling, undo/cancel; brush modifications of analytic masks | 8 masks, 64 strokes/mask, 4096 dabs/stroke, 65,536 dabs/mask; coverage raster capped at 1024px long edge even for export; physical pen hardware unverified |
| Cloning | 32 feathered source stamps with source selection | Not healing, content-aware or generative removal |
| Comparison | Original view and draggable before/after divider | No reference-photo or multi-photo survey mode |
| Transactions | Gesture-coalesced undo/redo, atomic selected-photo settings, named versions | Session-only 100-step undo; imports and album structural changes are not undoable |
| XMP | Metadata subset, reported Camera Raw parameter/curve subset, review-before-apply, native extension and lossless LightSpace settings round trips | No full Adobe preset/profile/mask/crop processing compatibility; unknown properties not retained; no interactive Adobe-app interoperability certification |
| Output | JPEG/PNG/WebP, quality/size, ZIP batch, catalog and XMP sidecars | Image output is 8-bit sRGB, maximum 8192px long edge, source EXIF/IPTC not embedded; no print, soft proof or tethering |
| Recovery | Revision-aware single writer, committed snapshots, retry, protected unreadable data, IndexedDB/native recovery, unload warning | Single JSON record including originals; no encrypted vault, journal, cross-tab merge, cloud sync or guaranteed close-time flush |
| Performance | Pixel-aware caches, stable UI cards, neutral bypass, bounded decode/Auto sampling, cached floating-point curves, incremental brush rasterization, weak diagnostic registrations | CPU decode/export/recovery and brush texture publication remain synchronous; budgets do not bound all peak memory or GPU allocations |
| GPU | Runtime-effect integration in Uno's Skia canvas | No separate WebGPU compute graph; host/device-dependent acceleration; SwiftShader tests are not physical-GPU certification |
| Delivery | Updated Actions and Playwright, engine/browser checks, desktop matrix, provenance-checked Pages, NuGet artifacts and release workflow | No NuGet.org publication, signed installers, notarization or auto-update service |

## Compatibility guarantees in this implementation

Original encoded bytes are not modified by development or sidecar operations. Curve/brush edits participate in undo, selected-photo synchronization, versions and recovery. New catalogs use schema 3; schemas 1 and 2 migrate with neutral defaults. Unsupported future schemas are rejected. XMP import reports unsupported Camera Raw fields instead of silently claiming equivalent results.

## Next production-level areas

RAW decoding and scene-linear floating-point color management, calibrated camera/lens profiles, full-resolution tiled rendering, asynchronous scheduling, incremental durable source storage and indexed catalog operations remain substantial separate workstreams. AI inference, panorama/HDR merging, print/proofing and cloud workflows are also absent. Adding controls for those workflows without real processing would not close their parity boundary.

See [advanced editing and XMP semantics](ADVANCED-EDITING.md), [performance evidence](PERFORMANCE.md), [recovery](RECOVERY.md) and [architecture](ARCHITECTURE.md).
