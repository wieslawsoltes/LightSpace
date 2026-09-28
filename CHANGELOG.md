# Changelog

## 0.4.0-alpha.1

### Sampled color-range masks

Added five-source-color selection with click replacement, Shift-add, Alt-remove, swatch removal, tolerance/smoothness and existing-mask restriction. Selection uses Oklab neighborhoods against source sRGB before clone/development, intersects spatial/brush/luminance coverage before inversion, and participates in undo, versions, native XMP and recovery. Sampling reads a small patch of the retained source preview; it does not copy the entire decoded image. Coverage remains a viewport-only aid.

### Content-addressed recovery

Originals are SHA-256-addressed immutable blobs; a separate manifest records committed metadata and source references. Warm edits avoid source writes, source-value reads and hashing. IndexedDB publishes new blobs and the manifest in one transaction; native storage stages blobs before replacing the manifest. Missing/corrupt sources fail restore, recovery is protected, failed commits stay dirty and retries restage originals. Legacy recovery remains readable; portable catalogs still embed originals. Unreferenced sources are retained, not automatically collected.

Hardened browser publication against synchronous exceptions after writes have been queued, invalid/duplicate photo identities, conflicting source lengths and changed content hashes. A blocked database open closes a connection that succeeds after its promise was rejected. Browser transactions request strict durability and acknowledge success only on completion. These are not cross-tab merge or power-loss-proof journaling guarantees.

### Compatibility and verification

Catalog schema 4 migrates schemas 1–3. Native XMP version 4 accepts version 3; the browser database upgrades to version 2. Added scalar and actual-pixel selection tests, crop/rotation-aware source picking, sample-patch/cache assertions, integrity and atomic-publication checks, real-browser fault injection, missing-source retry/protection and legacy migration. Added machine-readable recovery work counters and scoped microbenchmarks.

## 0.3.0-alpha.1

Added arbitrary-point master/R/G/B curves, shape-preserving interpolation, numeric/pointer editing and a separately cached floating-point lookup table. Added add/erase brush masks with pressure-aware dabs, feather/flow/density, arc-length resampling, incremental affine coverage caching and gesture undo/cancel.

Added bounded XMP import/export with metadata, a reported Camera Raw parameter/curve subset, review-before-apply, metadata-only mode and a complete native LightSpace settings extension. Added schema 3 migration. Merged Actions/Playwright updates, split workspace partials, replaced strong diagnostic registrations with weak references and excluded stroke-coordinate arrays from diagnostics.

## 0.2.0-alpha.1

Added four-way grading, rotatable gradients/handles, luminance ranges, coverage overlay, local tone/color, mask management and a draggable comparison divider. Added schema 2 migration, direct value comparisons, pixel-aware caches, stable photo cards/inspectors, bounded thumbnail/Auto sampling, neutral shader bypasses and opt-in diagnostics.

## 0.1.1-alpha.1

Fixed autosave capturing uncommitted previews and acknowledging stale revisions. Added a reusable single-writer coordinator, explicit save/retry status, protected unreadable recovery, browser unload warning, slider cancellation and source/state-aware caches.

## 0.1.0-alpha.1

Initial shared Uno photography workspace and eight libraries: local catalog, original-byte retention, tonal/color editing, presets, crop, gradient masks, cloning, transactions, versions, export, recovery, custom controls and delivery workflows.
