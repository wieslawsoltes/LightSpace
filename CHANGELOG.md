# Changelog

## 0.2.0-alpha.1

### Photography features

Added shadows/midtones/highlights/global color grading, a reusable pointer/keyboard color wheel, hue/saturation/luminance controls, blending/balance and per-range reset. Grading participates in existing transactions, copy/sync, presets, versions, export and recovery. Added Teal & amber and Warm silver presets.

Added arbitrary-angle linear gradients and rotated radial masks with on-canvas move, resize and rotation/fade handles. Luminance-range masks and luminance restriction of spatial gradients use source sRGB brightness. Masks now support local contrast, temperature and tint, amount, enable/disable, rename, duplicate and deletion. Selected-mask coverage is visible as an optional overlay but is excluded from export and histogram processing. Duplicating a mask resolves its latest edited state, not stale inspector construction state.

The before/after divider is draggable and does not create a document transaction.

### Performance and architecture

Replaced serialization-based transaction comparisons with direct semantic equality. Separated metadata, shader-input and complete-pixel invalidation. Shader and thumbnail caches ignore metadata-only changes while still invalidating on source replacement. Neutral effects bypass unnecessary processing. Preview sources have configurable count/byte budgets; thumbnails have a separate 384-pixel decode target. Auto tone reads 6,144 pixels rather than a full managed preview array.

Catalog cards stay alive when page membership is unchanged. Sidebar and inspector reconstruction is limited to structural changes. Valid normalized arrays retain their identities. Unchanged slider values and button states avoid redundant text/track/chrome updates. Browser diagnostic tree walks are opt-in rather than part of normal sessions.

Added pixel/reference tests, grading and mask round trips, state-equality/caching tests, CPU allocation/timing reports and real browser tests for grading, mask handles/ranges/management, comparison, and metadata-update work counters.

### Catalog migration

New catalogs use schema2 to preserve grading and extended mask parameters. Schema1 imports migrate with neutral defaults. Older builds reject unsupported schema2 rather than silently discard new edits.

## 0.1.1-alpha.1

Fixed recovery capturing partial slider previews and stale save acknowledgements. Added a reusable committed-revision single-writer coordinator, retry, explicit footer save state, browser unsaved-change warnings, and protection against automatic replacement of unreadable recovery. Escape cancels slider gestures. Source/state identities invalidate caches correctly when a catalog reuses IDs and revision zero.

Added controlled asynchronous recovery and browser regression tests. The underlying source-retention, catalog, editing, crop, masks, clone, export and shared Uno hosts were retained.

## 0.1.0-alpha.1

Initial independent photography workspace with eight reusable libraries, custom Uno controls, source-preserving editing, tonal/color runtime shaders, local catalog/recovery, image/catalog export and native/browser CI/Pages workflows.
