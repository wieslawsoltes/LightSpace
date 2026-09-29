# Feature coverage and compatibility boundaries

**0.5.0-alpha.1** is a functional, independent Uno photography workspace. This ledger distinguishes implemented behavior from remaining Lightroom/UI/processing parity; it is not an Adobe compatibility certification.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Workspace | Original dark chrome, library, filmstrip, inspector/tool rail, resizable side panels, focus/filmstrip toggles, compact crop layout | Not pixel-identical Adobe chrome or all keyboard/accessibility parity; widths are session-only; phone layout remains limited |
| Reuse | Eight packable libraries; independent engine; public curve, grading, mask, optics, geometry, histogram and grip controls | Some composition remains in StudioView partials; standard Uno input/scroll/select primitives remain |
| Input | JPEG, PNG, WebP, BMP, first GIF frame; EXIF orientation and sRGB decode | No RAW/demosaic, DNG, HEIF/HEIC, TIFF, video or animation workflow |
| Catalog | Originals, albums, multi-selection, ratings/flags, captions/keywords, search/filtering, 60-photo pages | No durable indexed large catalog, folder watcher, relinking, stacks, face/location indexing or lrcat support |
| Development | Global tone, relative white balance and source picker, saturation/vibrance, monochrome, effects/detail approximations | Not calibrated camera Kelvin, scene-referred RAW or Adobe algorithm equivalence; no AI denoise |
| Curves | Legacy five-point plus master/R/G/B arbitrary points, numeric/keyboard/pointer editing, linear or shape-preserving interpolation | 32 points/channel, 2048-entry rendering lookup; no claim of Adobe interpolation equivalence |
| Grading/mixer | Four-way grading, blending/balance, tonal luminance; eight weighted HSV bands | Original models, not Adobe pixel parity; no point-color adjustment workflow |
| Optics | Manual distortion, linear-light radial falloff, red/cyan and blue/yellow radial alignment | Fixed-frame original model; no lens database, automatic calibration, calibrated defringe/profile correction |
| Geometry/crop | Manual vertical/horizontal perspective, -45° to +45° rotate, aspect, scale/offsets, conservative constrain crop, drawn-horizon straightening; crop handles/ratios/quarter turns/flips | No automatic/guided Upright solver or maximum-area crop optimization; ratio buttons are presets, not continuous aspect locks |
| Histogram/clipping | Five-region tone dragging, one-gesture undo/cancel, endpoint indicators, independent clipping triangles and J shortcut | Reduced-resolution CPU/raster analysis and LDR thresholds; no scene-linear HDR histogram or clipping recovery guarantees |
| Navigation/comparison | Source-pixel zoom geometry, fit/pan/wheel zoom, draggable before/after divider retaining corrected framing | Bounded preview, not tiled native-resolution inspection; no reference-photo/survey view |
| Gradient/range masks | Rotatable gradients/handles, luminance and five-color Oklab ranges, intersections, coverage, amount/enable/invert and local tone/color; corrected source mapping | Eight masks; no AI selection, region-drag sampler or arbitrary boolean mask-group graph |
| Brush masks | Paint/erase, pressure-aware radius/flow, feather/density, arc-length sampling, undo/cancel and analytic-mask modifications; corrected cursor geometry | 64 strokes/mask, 4096 dabs/stroke, 65,536 dabs/mask; coverage capped at 1024px including export; physical pen unverified |
| Clone | 32 feathered source stamps | No healing, content-aware or generative removal |
| Transactions | Gesture-coalesced and batch edits, named versions, geometry/optics synchronization, protected recovery snapshots | 100-step session-only undo; imports and album structural operations not undoable |
| XMP | Metadata subset, reported Camera Raw scalar/curve subset, review-before-apply, native full-settings round trips | No full Adobe profile/preset/mask/geometry processing equivalence; unknown properties are not preserved |
| Output | JPEG/PNG/WebP, quality/size, batch ZIP, catalog and sidecars; geometry/optics included, overlays excluded | 8-bit sRGB, 8192px long edge, source EXIF/IPTC not embedded; raster encoding; no print/soft proof/tethering |
| Recovery | Revision-aware source-separated SHA-256 storage, atomic manifests, integrity checks, retry and unreadable-data protection | No encryption, journal, cross-tab merge, orphan cleanup, cloud sync or guaranteed close flush |
| GPU/performance | Composed development/optical shaders and independent cached draw geometry; pixel-aware caches, no CPU intermediate between display stages, weak inverse mapping cache | Host-dependent GPU execution; no separate WebGPU compute engine; decode, brush preparation, histogram and export retain CPU work; CA can multiply child evaluations |
| Delivery | Build/browser/desktop/Pages checks, SourceLink/symbol packages, six-RID single-file releases, NuGet Trusted Publishing configuration | No signed/notarized installers or automatic updater; artifact creation is not proof of package publication |

## Compatibility

Original encoded bytes are not changed by editing or sidecars. New corrections participate in undo, synchronization, versions, native XMP and recovery. Catalog schema 5 accepts versions 1–4 through migration; native XMP settings version 5 accepts versions 3–4. Unsupported future schemas fail rather than silently discard data. The recovery manifest format remains 1 and IndexedDB remains version 2. Retain older portable backups for interoperability with older applications.

## Validation boundaries

Tests use synthetic images, mathematical references, actual encoded outputs, native lifetime stress and real browser pointer/keyboard/file input. CI browser rendering uses SwiftShader; native jobs certify compilation rather than every driver, browser engine or pen device. Performance evidence identifies its scope and does not claim that every edit is GPU-only or reaches a universal frame rate.

[Optics/geometry and controls](OPTICS-GEOMETRY.md) · [Advanced editing/XMP](ADVANCED-EDITING.md) · [Performance](PERFORMANCE.md) · [Recovery](RECOVERY.md)
