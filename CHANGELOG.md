# Changelog

## 0.8.0-alpha.1

Added persistent virtual copies rooted directly at their original, with independent processing/metadata, unique family names, reversible batch creation, copy-only removal, rename, album-order and selection preservation. Copy-of-copy creation resolves to the original root. Original/copy filters, a paged family manager, Survey comparison, copy labels and distinct export filenames complete the workflow.

Portable catalog schema 6 stores each family's encoded source once. Loading validates master identities, dimensions, family-name uniqueness and source consistency before hydrating a common source array. Recovery validates family/source references and reuses existing SHA-256 blobs; warm copy renames do not rehash originals or rebuild pixel caches. Invalid cycles, missing roots, conflicting bytes and mixed original/copy removals fail before mutation. Native XMP settings remain version 5; recovery manifest and database versions remain 1/2.

Reviewed and merged PR #15. Added release documentation and an automatic package/symbol provenance gate with twelve fault-injection tests. Existing API guides, SourceLink/symbol packaging, six-RID release and Trusted Publishing workflows are retained. Source/package creation is not a claim of NuGet.org publication. The shader-lifetime fix and all-slider stress remain enabled. Full Lightroom parity is not claimed.

## 0.7.0-alpha.1

Added multi-photo Survey culling with twelve aspect-aware previews per page, single-candidate rating/flags, keyboard activation/navigation, reversible exclusions and rejected-photo hiding. Exclusion never deletes catalog records or source files. Survey uses a separate bounded renderer with a 1024-pixel target and 48 MiB decoded budget, prepares one candidate per dispatcher tick, retains cards/layout/pixel caches through metadata changes, and releases previews/source references when closed.

Added layout/selection/targeted-edit, actual-pixel, warm cache, keyboard, compact-layout and recovery checks. Fixed Survey-to-Detail selection handoff so later metadata changes target the displayed photo. The existing crash regression remains enabled; no schema change was introduced.

## 0.6.0-alpha.1

Added a session-local frozen reference beside active editing, side-by-side/stacked layouts, linked/independent fit-relative navigation, pin/fit/close controls and selective reference matching. Matching immutable source arrays share a decoded image while retaining independent shaders. The reference adds no catalog record or recovery revision.

Added thirteen processing groups for copy/paste/sync with captured source/target review and one staged undoable transaction. Metadata and unselected groups are retained. Direct batch application avoids repeated catalog lookups. Source ownership survives single-slot eviction; RendererStatistics retains its six-field API. Linked pan survives resizing, and short dialogs fit their contents. No schema change.

## 0.5.0-alpha.1

Added manual distortion, linear-light lens-falloff correction and radial channel alignment; vertical/horizontal projective geometry, arbitrary rotation, aspect/scale/offsets and conservative constrained framing. Added drawn-horizon straightening, source-patch relative white balance, interactive five-region histogram/clipping indicators, custom panel resizing, focus and filmstrip controls.

Development feeds a composed optical effect without a CPU-rendered display intermediate; geometry is independently cached. Inverse mapping retains source attachment for masks and sampling. Schema 5 and native XMP 5 preserve optics/geometry; recovery manifest/database remain 1/2. Existing package/symbol/six-RID release workflows remain intact.

## 0.4.1-alpha.1

Fixed the reproduced WebAssembly slider-crash path by separating compiled-shader ownership from native input cleanup, deterministically disposing staging, retaining source/output lifetimes and simplifying neutral-state mixer comparison. No editing or accelerated-runtime feature was disabled.

Added forced-finalization/deferred-draw/independent-uniform regressions and a real-pointer stress sweep of all 17 development sliders, histogram updates, undo/redo, JPEG/catalog export and recovery reload. CI/Pages retain a combined report. Schema 4 and IndexedDB 2 were unchanged; loading the fix required reload, not deleting recovery.

## 0.4.0-alpha.1

Added five-color Oklab selections with replacement/Shift-add/Alt-remove, swatches, tolerance/smoothness and existing-mask restriction. Sampling reads a bounded source-preview patch before development; coverage remains viewport-only.

Introduced source-separated SHA-256 originals and committed manifests. Warm metadata writes avoid unchanged original values/hashing. Restore verifies integrity; invalid data is protected and failed writes stay dirty. Browser publication explicitly aborts synchronous failures; native publication stages sources before the manifest. Schema 4 migrates 1–3; orphan sources remain retained.

## 0.3.0-alpha.1

Added arbitrary master/R/G/B curves, shape-preserving interpolation, numeric/pointer editing and separate floating-point lookup caching. Added pressure-aware paint/erase brushes, arc-length resampling, incremental affine coverage and gesture undo/cancel.

Added bounded XMP metadata/Camera Raw subset reporting, review-before-apply, metadata-only import and complete native settings. Schema 3 migrates 1–2. Merged Actions/Playwright updates, split workbench partials and weakened diagnostic ownership.

## 0.2.0-alpha.1

Added four-way grading, reusable color controls, rotatable gradients/handles, luminance ranges, coverage, local adjustments, mask management and comparison divider. Schema 2 migration accompanied pixel-aware invalidation, direct comparisons, stable cards, bounded thumbnails/Auto and opt-in diagnostics.

## 0.1.1-alpha.1

Fixed autosave of uncommitted previews and stale acknowledgements. Added revision-aware single-writer recovery, explicit save/retry, protected corrupt data, unload warnings, slider cancellation and source/state-aware caches, with asynchronous/browser regressions.

## 0.1.0-alpha.1

Initial shared Uno photography workspace and eight reusable libraries: catalog, retained originals, tonal/color editing, presets, crop, gradients, clones, transactions, versions, export/recovery and custom native/browser controls.
