# Feature coverage and compatibility boundaries

LightSpace 0.8.0-alpha.1 is an independent functional Uno photography workspace, not a claim of exact Lightroom UI, processing, file-format or complete feature parity. Virtual copies extend Survey, reference comparison and the optical/projective foundation.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Shell | Custom dark workspace, library/filmstrip/inspector/tool rails, panel resize grips, focus and filmstrip toggles | Not pixel-identical Adobe UI; no Lightroom Classic module suite; layout state is session-local |
| Reuse | Eight packable libraries; reusable optics/geometry/histogram, grading/mask/curve, reference/settings and Survey controls; copy family models and reversible editing operations | Some workbench composition remains in partial classes; standard Uno input/scroll/select primitives remain |
| Input | JPEG, PNG, WebP, BMP and first GIF frame; orientation-normalized sRGB decoding | No RAW/demosaic, DNG, HEIF/HEIC, TIFF, video or animated workflows |
| Catalog | Albums, selection, ratings/flags, captions/keywords, search/filtering, 60-photo pages; persistent virtual copies, family manager and original/copy filters | No persistent indexed database, watching/relinking, stacks, master promotion, face/location indexing or lrcat import |
| Virtual copies | Independent processing/metadata, direct master relationships, unique names, batch creation, copy-only removal/undo, album preservation and compact shared-source persistence | No Set Copy As Master, automatic stacking, Adobe catalog interchange or deletion of original files through this operation; copies count toward 5,000 records |
| Global development | Tone, relative white balance and source-patch picker, saturation/vibrance, monochrome, effects/detail approximations | Original LDR processing; no calibrated camera profiles, AI denoise or RAW white balance |
| Curves | Legacy five-point plus arbitrary master/R/G/B, linear or shape-preserving interpolation, numeric/pointer/keyboard editing | 32 points/channel; sampled 2048-entry render lookup; no Adobe interpolation equivalence |
| Grading/mixer | Four-way grading, blending/balance, tonal luminance; eight weighted HSV bands | Original models, not Adobe pixel parity; no point-color adjustment workflow |
| Optics | Manual distortion, linear-light radial falloff, red/cyan and blue/yellow radial alignment | Fixed-frame original model; no lens database, automatic calibration, calibrated defringe/profile correction |
| Geometry/crop | Manual perspective, rotation, aspect, scale/offsets, conservative constrain crop, drawn-horizon straightening; crop handles/ratios/quarter turns/flips | No automatic/guided Upright solver or maximum-area crop optimization; ratio buttons are presets, not continuous aspect locks |
| Histogram/clipping | Five-region tone dragging, gesture undo/cancel, endpoint indicators, independent clipping triangles and J shortcut | Reduced-resolution CPU/raster analysis and LDR thresholds; no scene-linear HDR histogram |
| Navigation/comparison | Fit/source-pixel geometry, pan/wheel zoom, before/after divider; frozen reference with linked/independent navigation; paged Survey culling and family comparison | Bounded preview, not tiled native-resolution inspection; linked navigation is fit-relative, not registered alignment; reference/Survey are session-local; twelve 1024px previews per Survey page |
| Gradient/range masks | Rotatable gradients/handles, luminance and five-color Oklab ranges, intersections, coverage, amount/enable/invert and local tone/color; corrected source mapping | Eight masks; no AI selection, region-drag sampler or arbitrary boolean mask-group graph |
| Brush masks | Paint/erase, pressure-aware radius/flow, feather/density, arc-length sampling, undo/cancel and analytic-mask modifications | Coverage capped at 1024px including export; physical pen unverified; serialized stroke limits remain enforced |
| Clone | 32 feathered source stamps | No healing, content-aware or generative removal |
| Transactions/settings | Gesture and batch edits, named versions, 13-group selective copy/paste/sync; reversible copy creation/rename/removal; metadata retained during settings transfer | 100-step session-only history; imports and album structural operations not undoable; no semantic mask adaptation or automatic exposure matching |
| XMP | Metadata subset, reported Camera Raw scalar/curve subset, review-before-apply, native full-settings round trips for the selected look | Copy family identity is catalog-only; no full Adobe profile/preset/mask/geometry equivalence; unknown properties not preserved |
| Output | JPEG/PNG/WebP, quality/size, batch ZIP, compact family catalogs and sidecars; distinct copy filenames; overlays excluded | 8-bit sRGB, 8192px long edge, source EXIF/IPTC not embedded; raster encoding; no print/soft proof/tethering |
| Recovery | Revision-aware SHA-256 storage, atomic manifests, source and family validation, retry and unreadable-data protection | No encryption, journal, cross-tab merge, orphan cleanup, cloud sync or guaranteed close flush |
| GPU/performance | Composed development/optical effects, cached geometry, source sharing across processing identities, metadata-aware invalidation, staged bounded Survey previews | Host-dependent GPU; no separate WebGPU compute engine; decode, brush preparation, histogram/export retain CPU work; separate renderers retain separate preview budgets |
| Delivery | Engine/browser/desktop/Pages checks, automatic eight-package/symbol provenance audit, SourceLink, six-RID single-file releases, NuGet Trusted Publishing configuration | No signed/notarized installers or updater; artifact creation is not proof of NuGet publication |

## Compatibility

Original encoded bytes are unchanged by editing or sidecars. Catalog schema 6 accepts versions 1–5 and preserves direct master/copy relations; compact portable copies hydrate their source from the original record. Invalid roots, cycles, inconsistent payloads/dimensions or duplicate family names fail rather than lose data. Native XMP settings remain version 5 and accept versions 3–4 because processing fields are unchanged. Recovery manifest format 1 and IndexedDB version 2 remain unchanged. Older builds reject catalog schema 6; keep portable pre-upgrade backups. Do not clear site data to load this release.

## Validation boundaries

Tests use synthetic images, mathematical references, actual encoded outputs, native lifetime stress and browser pointer/keyboard/file input. CI uses Chromium/SwiftShader; native jobs certify compilation, not every GPU driver, browser engine or pen device. Work counters and raster microbenchmarks do not imply GPU-only processing or a universal frame rate. Package audits validate archive metadata/provenance and required payloads, not installed execution or publication status.

[Virtual copies](VIRTUAL-COPIES.md) · [Survey](SURVEY.md) · [Reference/settings](REFERENCE-AND-SYNC.md) · [Optics/geometry](OPTICS-GEOMETRY.md) · [Advanced editing/XMP](ADVANCED-EDITING.md) · [Performance](PERFORMANCE.md) · [Recovery](RECOVERY.md)
