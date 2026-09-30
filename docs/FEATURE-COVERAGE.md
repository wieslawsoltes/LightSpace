# Feature coverage and compatibility boundaries

LightSpace 0.9.0-alpha.1 is an independent functional Uno photography workspace, not exact Lightroom UI, processing, file-format or complete feature parity. Continuous cropping and composition guides extend virtual copies, Survey, reference comparison and the optical/projective foundation.

| Area | Implemented | Remaining boundary |
| --- | --- | --- |
| Shell | Custom dark workspace, rails, filmstrip, inspector, panel grips, focus and filmstrip toggles | Not pixel-identical Adobe UI; no Classic module suite; layout is session-local |
| Reuse | Eight libraries; independent engine; public crop math/guides and aspect editor, optics/geometry, histogram, grading/masks/curves, reference/settings/Survey and copy-family APIs | Some workbench composition remains in partial classes; standard Uno primitives remain |
| Input | JPEG, PNG, WebP, BMP and first GIF frame; orientation-normalized sRGB decoding | No RAW/demosaic, DNG, HEIF/HEIC, TIFF, video or animated workflows |
| Catalog/copies | Albums, selection, metadata filters, 60-photo pages; persistent virtual-copy families, shared originals, safe copy-only removal/undo, preserved album/selection ordering | No indexed persistent catalog, relinking, stacks, master promotion, face/location indexing or lrcat import; 5,000 records includes copies |
| Development | Tone, relative white balance and bounded source picker, saturation/vibrance, monochrome and effects/detail approximations | Original LDR algorithms; no calibrated profiles, AI denoise or RAW white balance |
| Curves/grading/mixer | Master/R/G/B arbitrary curves, fixed five-point compatibility, four-way grading and eight weighted HSV bands | 32 points/channel, 2048-entry lookup; no Adobe interpolation or pixel equivalence, point-color workflow absent |
| Optics/geometry | Manual distortion/falloff/channel alignment; perspective, rotate, aspect, scale/offset and conservative constrain crop | No calibrated lens database, automatic/guided Upright or maximum-area crop optimization |
| Crop | Continuous locked/free bounds, output-aware preset/custom ratios, all corner/edge anchors, centered and temporary-lock gestures, orientation swap, nudging, horizon straightening, quarter turns/flips and capture cancellation | Existing one-percent minimum extents; policies fixed at pointer-down; tool lock and guide choices are session-local |
| Composition guides | Thirds, Grid, Golden ratio, Diagonals, reversible Triangle and None, inspector controls and keyboard cycling; bounded allocation-free line generation | No golden spiral, custom print-aspect overlay or configurable cycle; no guide export |
| Histogram/clipping | Five-region dragging, undo/cancel, clipping triangles and J shortcut | Reduced-resolution raster/CPU analysis, LDR thresholds rather than RAW/HDR recoverability |
| Comparison/Survey | Fit/source-pixel geometry, pan/zoom, before-after, frozen reference, linked or independent navigation, twelve-candidate paged Survey, family culling and safe exclusions | Bounded previews, no native-resolution tiling or image registration; reference/Survey state is session-local |
| Masks | Rotatable gradients, luminance/five-color Oklab restrictions, coverage, local adjustments, enable/invert/amount; pressure-aware add/erase brushes | Eight masks, 1024px brush coverage including export; no AI selections or arbitrary mask-group graph; pen hardware unverified |
| Clone | 32 feathered source stamps | No healing, content-aware or generative removal |
| History/settings | Gesture/batch transactions, versions, thirteen-group selective transfer, reversible copy operations | 100-step session history; imports/album structure not undoable; no semantic mask adaptation or automatic exposure matching |
| XMP | Metadata and reported Camera Raw scalar/curve subset, reviewed import and full native processing round trips | Family identity is catalog-only; no full Adobe profile/preset/mask/geometry equivalence; unknown fields not retained |
| Output | JPEG/PNG/WebP, quality/size, batch ZIP, compact family catalogs and sidecars; overlays excluded | 8-bit sRGB, 8192px long edge, source EXIF/IPTC not embedded; CPU/raster encoding; no print/proof/tether |
| Recovery | Source-separated SHA-256 originals, atomic committed manifests, integrity/family validation, retry and unreadable-data protection | No encryption, journal, multi-tab merge, orphan cleanup, cloud sync or guaranteed close flush |
| Performance/GPU | Composed effects, cached geometry, source sharing, metadata-aware invalidation, bounded Survey preparation and allocation-free analytic crop/guide loop | Host-dependent Skia GPU; no WebGPU compute backend; decoding, brush preparation, histogram/export retain CPU work; separate renderers have separate budgets |
| Distribution | Engine/browser/desktop/Pages gates, eight package/symbol audits, SourceLink, six-RID release packaging and Trusted Publishing workflow | No signed/notarized installers or updater; build artifacts do not establish publication |

## Compatibility

Original encoded bytes remain unchanged. Catalog schema 6 migrates versions 1–5 and preserves validated copy families. Native XMP 5 accepts 3–4. Recovery manifest 1 and IndexedDB 2 are unchanged. Crop constraints persist only their existing numeric bounds; locks, guides and custom input are session-local. Reload without clearing site data. Older builds reject unsupported schemas; keep pre-upgrade portable backups when using them.

## Validation scope

Tests use mathematical references, synthetic and encoded images, forced-finalization/deferred-draw checks, actual Uno pointer/keyboard/file operations and recovery inspection. Crop checks validate exact restoration, anchor geometry, output dimensions and cached resources; they do not certify every display/driver. Chromium/SwiftShader and native compilation are not physical-GPU, pen or all-browser certification. Work counters and raster timings do not imply GPU-only execution or universal frame rates.

[Crop](CROP-CONSTRAINTS.md) · [Virtual copies](VIRTUAL-COPIES.md) · [Survey](SURVEY.md) · [Reference](REFERENCE-AND-SYNC.md) · [Optics/geometry](OPTICS-GEOMETRY.md) · [Advanced editing](ADVANCED-EDITING.md) · [Recovery](RECOVERY.md)
