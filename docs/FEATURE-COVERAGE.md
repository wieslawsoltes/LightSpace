# Feature coverage and compatibility boundaries

LightSpace is an independent functional Uno photography workspace, not a claim of exact Lightroom UI, processing, file-format or complete feature parity. The Survey increment extends reference comparison and the optical/projective foundation.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Shell | Custom dark photo workspace, library/filmstrip/inspector/tool rails, panel resize grips, focus and filmstrip toggles | Not pixel-identical Adobe UI; no Lightroom Classic module suite; layout state is session-local |
| Reuse | Eight packable libraries and independent engine; reusable geometry/optics/histogram, grading/mask/curve, reference view and settings-selection controls | Some workbench composition remains in partial classes; standard Uno input/scroll/select primitives remain |
| Input | JPEG, PNG, WebP, BMP and first GIF frame; orientation-normalized sRGB decoding | No RAW/demosaic, DNG, HEIF/HEIC, TIFF, video or animated workflows |
| Catalog | Original bytes, albums, selection, ratings/flags, captions/keywords, search/filtering and 60-photo pages | No persistent indexed catalog, watching/relinking, stacks, virtual copies, face/location indexing or lrcat import |
| Global development | Tone, relative white balance and source-patch picker, saturation/vibrance, monochrome, effects/detail approximations | Original LDR processing; no calibrated camera profiles, AI denoise or RAW white balance |
| Curves | Legacy five-point plus arbitrary master/R/G/B, linear or shape-preserving interpolation, numeric/pointer/keyboard editing | 32 points/channel; sampled 2048-entry render lookup; no Adobe interpolation equivalence |
| Grading/mixer | Four-way grading, blending/balance, tonal luminance; eight weighted HSV bands | Original models, not Adobe pixel parity; no point-color adjustment workflow |
| Optics | Manual distortion, linear-light radial falloff, red/cyan and blue/yellow radial alignment | Fixed-frame original model; no lens database, automatic calibration, calibrated defringe/profile correction |
| Geometry/crop | Manual perspective, rotation, aspect, scale/offsets, conservative constrain crop, drawn-horizon straightening; crop handles/ratios/quarter turns/flips | No automatic/guided Upright solver or maximum-area crop optimization; ratio buttons are presets, not continuous aspect locks |
| Histogram/clipping | Five-region tone dragging, gesture undo/cancel, endpoint indicators, independent clipping triangles and J shortcut | Reduced-resolution CPU/raster analysis and LDR thresholds; no scene-linear HDR histogram |
| Navigation/comparison | Fit/source-pixel geometry, pan/wheel zoom, before/after divider; frozen reference alongside active editing, side-by-side/stacked, linked/unlinked navigation and selective reference matching; paged multi-photo Survey culling with targeted rating/flags and reversible exclusions | Bounded preview, not tiled native-resolution inspection; linked navigation is fit-relative, not registered alignment; reference and Survey are session-local; Survey is fit-only with twelve 1024px previews per page |
| Gradient/range masks | Rotatable gradients/handles, luminance and five-color Oklab ranges, intersections, coverage, amount/enable/invert and local tone/color; corrected source mapping | Eight masks; no AI selection, region-drag sampler or arbitrary boolean mask-group graph |
| Brush masks | Paint/erase, pressure-aware radius/flow, feather/density, arc-length sampling, undo/cancel and analytic-mask modifications | Coverage capped at 1024px including export; physical pen unverified; serialized stroke limits remain enforced |
| Clone | 32 feathered source stamps | No healing, content-aware or generative removal |
| Transactions/settings | Gesture-coalesced and batch edits, named versions, 13-group selective copy/paste/sync, reviewed captured targets and one-transaction undo; metadata retained | 100-step session-only history; imports and album structural operations not undoable; no semantic mask adaptation or automatic exposure matching |
| XMP | Metadata subset, reported Camera Raw scalar/curve subset, review-before-apply, native full-settings round trips | No full Adobe profile/preset/mask/geometry equivalence; unknown properties are not preserved |
| Output | JPEG/PNG/WebP, quality/size, batch ZIP, catalog/sidecars; geometry/optics included, overlays excluded | 8-bit sRGB, 8192px long edge, source EXIF/IPTC not embedded; raster encoding; no print/soft proof/tethering |
| Recovery | Revision-aware source-separated SHA-256 storage, atomic manifests, integrity checks, retry and unreadable-data protection | No encryption, journal, cross-tab merge, orphan cleanup, cloud sync or guaranteed close flush |
| GPU/performance | Composed development/optical effects, cached geometry, no display-stage CPU intermediate; source sharing across reference/active with independent shaders; direct batch target application; staged Survey preparation and bounded twelve-source cache | Host-dependent GPU; no separate WebGPU compute engine; decode, brush preparation, histogram/export retain CPU work; extra views still add draw/shading work |
| Delivery | Build/browser/desktop/Pages checks, SourceLink/symbol packages, six-RID single-file releases, NuGet Trusted Publishing configuration | No signed/notarized installers or updater; artifact creation is not proof of package publication |

## Compatibility

Original encoded bytes are unchanged by editing or sidecars. Processing participates in undo, synchronization, versions, native XMP and recovery. Catalog schema 5 accepts versions 1–4; native XMP settings 5 accepts versions 3–4. Unsupported future schemas fail rather than discard data. Recovery manifest format 1 and IndexedDB version 2 are unchanged. Reference views and the settings clipboard are session-local and add no catalog records or schema fields.

## Validation boundaries

Tests use synthetic images, mathematical references, actual encoded outputs, native lifetime stress and browser pointer/keyboard/file input. CI browser rendering uses SwiftShader; native jobs certify compilation rather than every driver, browser engine or pen device. Resource counters and raster microbenchmarks are not claims that all edits are GPU-only or achieve a universal frame rate.

[Survey culling](SURVEY.md) · [Reference and selective settings](REFERENCE-AND-SYNC.md) · [Optics/geometry](OPTICS-GEOMETRY.md) · [Advanced editing/XMP](ADVANCED-EDITING.md) · [Performance](PERFORMANCE.md) · [Recovery](RECOVERY.md)
