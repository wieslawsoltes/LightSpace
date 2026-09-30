# Changelog

## 0.9.0-alpha.1

Adds continuous locked/free crop resizing, output-aware preset/custom ratios, anchor-preserving corners/edges, centered Alt/Option resizing, temporary Shift locking, A lock toggle, X orientation swap and pixel nudges. CropGuides emits bounded Thirds/Grid/Golden ratio/Diagonals/Triangle/None segments into caller-owned storage; O cycles and Shift+O reverses Triangle. Guides do not affect processing or export.

Crop gestures capture initial bounds, modifier policy, pointer, source and coordinate frame. Root metrics, display scale, visibility, surface size and root-relative movement invalidate capture. Listener ownership is bounded to the gesture; release cannot commit after cancellation. Input refreshes the frame from arranged dimensions instead of relying on a previous paint. Resize acceptance now waits for actual Uno cancellation before releasing the pointer; additional tests cover lock cancellation and focus-layout changes followed by a new valid drag.

Crop-only changes reuse source/development/optical/curve caches. The warmed analytic benchmark checks 100,000 evaluations and guide writes with zero managed allocation in that loop. Full-frame crop state is shared rather than repeatedly allocated. Existing slider-crash, photography, copy, Survey, reference, sidecar and recovery tests remain enabled. Schema versions remain catalog 6, native XMP 5, manifest 1 and IndexedDB 2. Tool state is session-local. See docs/CROP-CONSTRAINTS.md for boundaries.

## 0.8.0-alpha.1

Added persistent virtual copies with direct roots, independent processing/metadata, family names, reversible creation/rename/copy-only removal, album-order and selection preservation. Original/copy filters, family manager, Survey, labels and distinct export names complete the workflow. Removal activates a selected survivor rather than displaying an unselected original.

Portable schema 6 stores each family's encoded source once and validates roots, names, dimensions and consistency before hydration. Recovery reuses SHA-256 sources; warm copy metadata changes retain pixel caches and source hashes/blobs. Native XMP remains 5 and recovery 1/2. Added automatic eight-package/symbol audits with twelve fault-injection tests. No tag or feed publication is implied by source versioning.

## 0.7.0-alpha.1

Added twelve-candidate paged Survey, aspect-aware layout, targeted ratings/flags, keyboard navigation, exclusions without catalog deletion and rejected-photo hiding. A separate 1024px/48 MiB renderer prepares one candidate per tick and releases resources on close. Metadata retains cards/layout/caches. Fixed Survey-to-Detail selection handoff. No schema change.

## 0.6.0-alpha.1

Added frozen reference alongside active editing, side-by-side/stacked layouts, linked/independent fit-relative navigation and selective matching. Shared sources retain independent processing identities. Added thirteen-group copy/paste/sync, captured reviews and atomic transactions retaining metadata. Direct target application avoids repeated scans. Ownership survives one-slot eviction; six-field statistics API retained. No schema change.

## 0.5.0-alpha.1

Added manual optics, projective geometry, conservative framing, horizon straightening, bounded relative white balance, histogram/clipping interaction, panel resizing, focus and filmstrip controls. Composed effects avoid a CPU-rendered display intermediate; geometry and inverse maps are cached. Catalog/native XMP 5 retain corrections; recovery remains 1/2. Six-RID and symbol/package delivery retained.

## 0.4.1-alpha.1

Fixed the reproduced WebAssembly slider crash through explicit shader ownership, deterministic staging cleanup, retained lifetimes and simpler neutral comparisons. Added forced-finalization/deferred-draw/uniform tests and all-seventeen-slider stress with export/reload validation. No feature or runtime acceleration disabled. Schema 4 and IndexedDB 2 unchanged.

## 0.4.0-alpha.1

Added five-color Oklab selection, bounded source picking, tolerance/smoothness and mask restrictions. Introduced SHA-256 originals and committed manifests; warm saves avoid source transfer/hashing, restore verifies integrity, failures stay dirty and damaged recovery is protected. Browser abort and native staged publication strengthened. Schema 4 migrates 1–3.

## 0.3.0-alpha.1

Added arbitrary RGB curves, shape-preserving interpolation, floating-point lookups and pressure-aware add/erase brushes with distance resampling and incremental affine coverage. Added bounded XMP subset/report/review, metadata-only import and native settings. Schema 3 migrates 1–2; weaker diagnostic ownership and split workbench partials reduce retention.

## 0.2.0-alpha.1

Added grading, rotatable gradients, luminance ranges, coverage, mask management and comparison divider. Schema 2 migration accompanied direct comparisons, pixel-aware caches, stable cards, bounded thumbnails/Auto and opt-in diagnostics.

## 0.1.1-alpha.1

Fixed preview autosave and stale acknowledgements. Added single-writer recovery, save/retry, protected damaged data, unload warnings, cancellation and source-aware caches.

## 0.1.0-alpha.1

Initial shared Uno application and eight libraries: catalog, retained originals, development, presets, crop, gradients, clones, history, versions, export/recovery and desktop/browser controls.
