# Changelog

## 0.4.1-alpha.1

Fixed the reproduced WebAssembly slider-crash path by separating compiled-shader ownership from native input cleanup, deterministically disposing uniform staging, retaining source/output lifetimes explicitly and using scalar mixer comparison in neutral transitions. No editing feature or accelerated runtime capability was disabled.

Added forced-finalization, deferred-draw and independent-uniform-snapshot regressions. The real-pointer stability test exercises all 17 development sliders, extremes/neutral crossings, histogram redraws, undo/redo, JPEG/catalog export and recovery reload. It verifies one transaction per gesture and every final saved value. Build and Pages retain a unified report, and build-info.json includes the application version.

Catalog schema 4 and IndexedDB version 2 are unchanged. Reload a crashed or old tab; do not delete local recovery to load this patch. See docs/WASM-SLIDER-FIX.md for reproduction evidence and the limits of the runtime diagnosis.

## 0.4.0-alpha.1

Added five-source-color selection with click replacement, Shift-add, Alt-remove, swatch removal, tolerance/smoothness and existing-mask restriction. Selection uses Oklab neighborhoods against source sRGB before clone/development, intersects spatial/brush/luminance coverage before inversion, and participates in undo, versions, native XMP and recovery. Sampling reads a small patch of the retained source preview; it does not copy the entire decoded image. Coverage remains a viewport-only aid.

Originals are SHA-256-addressed immutable blobs; a separate manifest records committed metadata and source references. Warm edits avoid source writes, source-value reads and hashing. IndexedDB publishes new blobs and the manifest in one transaction; native storage stages blobs before replacing the manifest. Missing/corrupt sources fail restore, recovery is protected, failed commits stay dirty and retries restage originals. Legacy recovery remains readable; portable catalogs still embed originals. Unreferenced sources are retained, not automatically collected.

Publication validates source lengths/hashes and duplicate identities, aborts synchronous failures after queued writes, and requests strict IndexedDB durability where supported. Color-swatch geometry remains stable and pointer tests require fresh diagnostic layout publications. Catalog schema 4 migrates versions 1–3; native XMP settings version 4 accepts version 3. Added source-selection pixel tests, integrity/atomic-publication tests and actual IndexedDB regressions. See the recovery and performance documentation for limits.

## 0.3.0-alpha.1

Added arbitrary-point master/R/G/B curves, shape-preserving interpolation, numeric and pointer editing, and an independently cached 2048-entry floating-point render lookup. Added add/erase brush masks with pressure-aware dabs, feather/flow/density, arc-length resampling, incremental affine coverage caching, gesture undo/cancel and brush modifications of existing analytic masks.

Added bounded XMP sidecar import/export with standard metadata, an explicitly reported Camera Raw parameter/curve subset, review-before-apply, metadata-only mode and an optional complete LightSpace settings extension. New catalog saves use schema 3; schemas 1 and 2 migrate with neutral defaults.

Merged the outstanding Actions and Playwright update PRs after validation. Split workbench layout/catalog/diagnostics responsibilities into separate partial files, removed strong diagnostic references to discarded controls, and excluded potentially large brush coordinate arrays from periodic opt-in diagnostics. Added engine/pixel/interchange/browser tests and work-avoidance counters. No RAW, AI or complete Lightroom compatibility is claimed.

## 0.2.0-alpha.1

Added shadows/midtones/highlights/global color grading, reusable grading and mixer controls, rotatable gradients with on-canvas handles, luminance-range masks and restrictions, coverage overlay, local color/tone settings, mask management and a draggable comparison divider. Added schema 2 migration.

Introduced allocation-free value comparisons, pixel-aware cache invalidation, stable photo cards/inspectors, bounded thumbnail decode and Auto sampling, neutral shader bypasses and opt-in diagnostics. Added actual-pixel, geometry, cache and real browser regression tests.

## 0.1.1-alpha.1

Fixed autosave capturing uncommitted previews and acknowledging stale revisions. Added a reusable single-writer recovery coordinator, explicit save/retry status, protected unreadable recovery, browser unload warning, slider cancellation and source/state-aware image caches. Added delayed-write, retry, preview/cancel and browser recovery regressions.

## 0.1.0-alpha.1

Initial shared Uno photography workspace and eight reusable libraries: local catalog, original-byte retention, tonal/color editing, curves/presets, crop, gradient masks, clone stamps, transactions, versions, export, recovery, custom controls and native/browser delivery workflows.
