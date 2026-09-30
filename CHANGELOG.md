# Changelog

## 0.7.0-alpha.1

Added paged multi-photo Survey culling with aspect-aware layouts, per-photo rating/flag controls, keyboard navigation, reversible view-only exclusions, rejected-photo hiding and return to Detail. Catalog multi-selection is preserved while metadata actions target one candidate with normal undo and recovery. No source or catalog deletion occurs through exclusion.

Survey renders directly through one Uno/Skia surface, with a separate twelve-photo 1024px preview cache. One candidate is prepared per dispatcher tick; this amortizes synchronous owner-thread work rather than claiming GPU/background decoding. Metadata changes retain cards, layout and processing caches. Leaving Survey releases its source references and decoded/brush/curve caches.

Added reusable layout/selection/culling/preparation APIs and engine/browser regressions for paging, geometry bounds, exclusion safety, single-target metadata, undo, cache reuse, resize and recovery. The original slider-crash checks remain enabled. Catalog schema 5, native XMP 5, recovery manifest 1 and IndexedDB 2 are unchanged. See docs/SURVEY.md.

## 0.6.0-alpha.1

Added a session-local frozen reference beside the active photograph, with side-by-side/stacked layouts, linked or independent fit-relative navigation, pin/fit/close controls, and selective application of the pinned look. The reference does not create a catalog photo or recovery revision. Both panes render through the existing composed Skia pipeline; matching immutable source arrays share one decoded image while retaining independent per-photo shaders.

Added thirteen independently selectable processing groups for copy, paste and synchronization. Dialogs capture the source look and target identities before review. Applications stage all target results and commit one undoable transaction, retaining metadata and unselected edits. Batch application no longer repeatedly scans the catalog for each target; undo/redo builds one identity lookup.

Linked normalized pan survives pane/window resizing. Content-sized dialogs keep short forms compact and long bodies scrollable. Added source-pool ownership, eviction, forced-finalization, deferred-draw, group-isolation, frozen-pixel and actual-browser comparison/synchronization regressions. RendererStatistics retains its six-field constructor/deconstruction while exposing unique source counts and reuse counters.

PR #13 completed this implementation on top of the 0.5 foundation. The shader-lifetime repair and all development-slider stress checks remain enabled. Catalog schema 5, native XMP 5, recovery manifest 1 and IndexedDB 2 are unchanged. Package guides, SourceLink/symbol metadata and publishing workflows are preserved. See docs/REFERENCE-AND-SYNC.md and docs/VALIDATION.md for behavior, evidence and limits.

## 0.5.0-alpha.1

### Photography tools and workspace

Added manual optical distortion, linear-light lens-falloff correction and radial channel alignment; projective vertical/horizontal correction, arbitrary rotation, aspect, scale/offsets and conservative constrained framing. Added on-canvas horizon straightening, a source-based relative white-balance picker, five-region histogram dragging, display-only clipping indicators, custom panel resizing, focus mode and filmstrip visibility controls.

Source/display mapping now includes inverse optical correction, projective geometry, crop and orientation for mask handles, brushes, clone positions and sampled colors. Compact crop controls and public GeometryEditor, OpticsEditor, HistogramView and PanelResizeGrip extend the reusable Uno components.

### Rendering and verification

Development feeds a composed presentation runtime effect directly, without an intermediate CPU image. Geometry is a separately cached draw matrix. Geometry-only gestures reuse decodes, development/optical shaders, curves and brush coverage; histogram analysis retains an independent presentation cache. Weak inverse-projection caching avoids repeated constrained-framing solves per outline point. The 0.4.1 ownership repair and full slider-crash stress remain enabled.

Added mathematical/pixel/lifetime/migration/synchronization tests and actual-browser correction, histogram, white-balance, panel-layout, export and recovery regressions. Performance reports distinguish actual resource counts and CPU/raster timings from physical-GPU certification.

### Compatibility and delivery

Catalog schema 5 and native XMP settings version 5 preserve optics/geometry. Catalogs 1–4 and native XMP 3–4 migrate with neutral missing fields. Recovery manifest format 1 and IndexedDB 2 are unchanged. Existing single-file six-RID desktop packaging, package metadata/symbols and NuGet Trusted Publishing workflows are preserved.

These manual original algorithms and UI improvements do not complete Lightroom parity. RAW/AI, calibrated camera/lens profiles, automatic Upright, HDR/panorama, tiled native-resolution rendering, printing/proofing and cloud workflows remain absent. CPU decode, brush texture preparation, histogram and export remain explicit.

## 0.4.1-alpha.1

Fixed the reproduced WebAssembly slider-crash path by separating compiled-shader ownership from input cleanup, deterministically disposing uniform staging, retaining source/output lifetimes and simplifying neutral-state mixer comparisons. No editing or accelerated-runtime feature was disabled.

Added forced-finalization/deferred-draw/independent-uniform regressions and a real-pointer stress test covering all 17 development sliders, histogram redraws, undo/redo, JPEG/catalog export and recovery reload. CI/Pages retain a unified report and build-info includes version. Schema 4 and IndexedDB 2 were unchanged; loading the fix required reload, not deleting recovery.

## 0.4.0-alpha.1

Added five-color Oklab selections with click replacement, Shift-add, Alt/swatches removal, tolerance/smoothness and existing-mask restriction. Sampling reads a bounded source-preview patch and selection remains before development. Masks participate in undo, versions, native XMP and recovery; overlay stays view-only.

Introduced source-separated SHA-256 originals and committed manifests. Warm metadata writes avoid unchanged original values/hashing, and restore verifies integrity. Browser publication stages sources/manifests transactionally with explicit synchronous-failure abort; native publication stages source files first. Corrupt data is protected, failed writes stay dirty and retry restages sources. Schema 4 migrated 1–3; orphan sources remained retained.

## 0.3.0-alpha.1

Added arbitrary master/R/G/B curves, shape-preserving interpolation, numeric/pointer editing and separate floating-point lookup caching. Added pressure-aware add/erase brushes, arc-length resampling, incremental affine coverage, gesture undo/cancel and modifications of existing masks.

Added bounded XMP metadata/Camera Raw subset with compatibility reporting, review-before-apply, metadata-only import and complete native settings. Schema 3 migrated 1–2. Merged Actions/Playwright updates, split workbench partials, weakened diagnostic ownership and excluded large stroke-coordinate payloads.

## 0.2.0-alpha.1

Added four-way grading, reusable color controls, rotatable gradients/handles, luminance ranges, coverage, local adjustments, mask management and comparison divider. Schema 2 migration accompanied pixel-aware invalidation, direct state comparisons, stable cards, bounded thumbnails/Auto and opt-in diagnostics.

## 0.1.1-alpha.1

Fixed autosave of uncommitted previews and stale acknowledgements. Added revision-aware single-writer recovery, explicit save/retry, protected corrupt data, unload warnings, slider cancellation and source/state-aware caches, with asynchronous and browser regressions.

## 0.1.0-alpha.1

Initial shared Uno workspace and eight reusable libraries: local catalog, retained originals, tonal/color editing, presets, crop, gradients, clones, transactions, versions, export/recovery and custom desktop/browser controls.
