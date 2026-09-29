# Changelog

## 0.5.0-alpha.1

### Photography tools and workspace

Added manual optical distortion, linear-light lens-falloff correction and radial channel alignment; projective vertical/horizontal correction, arbitrary rotation, aspect, scale/offsets and conservative constrained framing. Added on-canvas horizon straightening, a source-based relative white-balance picker, five-region histogram dragging, display-only clipping indicators, custom panel resizing, focus mode and filmstrip visibility controls.

Source/display mapping now includes inverse optical correction, projective geometry, crop and orientation for mask handles, brushes, clone positions and sampled colors. Compact crop controls and public GeometryEditor, OpticsEditor, HistogramView and PanelResizeGrip extend the reusable Uno components.

### Rendering and verification

Development feeds a composed presentation runtime effect directly, without an intermediate CPU image. Geometry is a separately cached draw matrix. Geometry-only gestures reuse decodes, development/optical shaders, curves and brush coverage; histogram analysis retains an independent presentation cache. Weak inverse-projection caching avoids repeated constrained-framing solves per outline point. The 0.4.1 ownership repair and full slider-crash stress remain enabled.

Added mathematical/pixel/lifetime/migration/synchronization tests and actual-browser correction, histogram, white-balance, panel-layout, export and recovery regressions. Performance reports distinguish actual resource counts and CPU/raster timings from physical-GPU certification.

### Compatibility and delivery

Catalog schema5 and native XMP settings version5 preserve optics/geometry. Catalogs1–4 and native XMP3–4 migrate with neutral missing fields. Recovery manifest format1 and IndexedDB2 are unchanged. Existing single-file six-RID desktop packaging, package metadata/symbols and NuGet Trusted Publishing workflows are preserved.

These manual original algorithms and UI improvements do not complete Lightroom parity. RAW/AI, calibrated camera/lens profiles, automatic Upright, HDR/panorama, tiled native-resolution rendering, printing/proofing and cloud workflows remain absent. CPU decode, brush texture preparation, histogram and export remain explicit.

## 0.4.1-alpha.1

Fixed the reproduced WebAssembly slider-crash path by separating compiled-shader ownership from input cleanup, deterministically disposing uniform staging, retaining source/output lifetimes and simplifying neutral-state mixer comparisons. No editing or accelerated-runtime feature was disabled.

Added forced-finalization/deferred-draw/independent-uniform regressions and a real-pointer stress test covering all17 development sliders, histogram redraws, undo/redo, JPEG/catalog export and recovery reload. CI/Pages retain a unified report and build-info includes version. Schema4 and IndexedDB2 were unchanged; loading the fix required reload, not deleting recovery.

## 0.4.0-alpha.1

Added five-color Oklab selections with click replacement, Shift-add, Alt/swatches removal, tolerance/smoothness and existing-mask restriction. Sampling reads a bounded source-preview patch and selection remains before development. Masks participate in undo, versions, native XMP and recovery; overlay stays view-only.

Introduced source-separated SHA-256 originals and committed manifests. Warm metadata writes avoid unchanged original values/hashing, and restore verifies integrity. Browser publication stages sources/manifests transactionally with explicit synchronous-failure abort; native publication stages source files first. Corrupt data is protected, failed writes stay dirty and retry restages sources. Schema4 migrated1–3; orphan sources remained retained.

## 0.3.0-alpha.1

Added arbitrary master/R/G/B curves, shape-preserving interpolation, numeric/pointer editing and separate floating-point lookup caching. Added pressure-aware add/erase brushes, arc-length resampling, incremental affine coverage, gesture undo/cancel and modifications of existing masks.

Added bounded XMP metadata/Camera Raw subset with compatibility reporting, review-before-apply, metadata-only import and complete native settings. Schema3 migrated1–2. Merged Actions/Playwright updates, split workbench partials, weakened diagnostic ownership and excluded large stroke-coordinate payloads.

## 0.2.0-alpha.1

Added four-way grading, reusable color controls, rotatable gradients/handles, luminance ranges, coverage, local adjustments, mask management and comparison divider. Schema2 migration accompanied pixel-aware invalidation, direct state comparisons, stable cards, bounded thumbnails/Auto and opt-in diagnostics.

## 0.1.1-alpha.1

Fixed autosave of uncommitted previews and stale acknowledgements. Added revision-aware single-writer recovery, explicit save/retry, protected corrupt data, unload warnings, slider cancellation and source/state-aware caches, with asynchronous and browser regressions.

## 0.1.0-alpha.1

Initial shared Uno workspace and eight reusable libraries: local catalog, retained originals, tonal/color editing, presets, crop, gradients, clones, transactions, versions, export/recovery and custom desktop/browser controls.
