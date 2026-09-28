# Changelog

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
