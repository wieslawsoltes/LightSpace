# Changelog

## 0.1.1-alpha.1

### Recovery correctness

An earlier committed edit could schedule autosave while a later slider gesture was still in progress. The old writer serialized that visible preview, and an old “saved” message could remain visible after a newer edit. Reloading at that point could restore a partial slider value.

Recovery now uses a reusable revision-aware single-writer coordinator. It captures committed snapshots without changing the visible preview, acknowledges only completed writes, drains newer committed revisions after an in-flight write, coalesces overlapping flush requests, and retains dirty state after failures. A dedicated footer control reports save state and offers retry. Browser navigation warns about unsaved work. Escape cancels slider gestures and releases pointer capture.

Unreadable recovery is protected from automatic replacement. The user must explicitly confirm replacement through the save control. This is not a repair tool for damaged catalogs; it preserves the old recovery rather than overwriting it with sample images.

### Rendering correctness

Decoded-image and runtime-shader caches validate source/state identity in addition to photo ID and revision. Thumbnail caches use the same identity checks and are cleared when opening a catalog. Reopening data that reuses IDs and revision zero no longer reuses stale pixels or settings.

### Validation

Added controlled asynchronous recovery tests for delayed writes, concurrent flushes, failed-write retry, previews, cancellation, no-op gestures, catalog replacement, immutable serialized payloads, synchronous writers and disposal. Added actual-pixel renderer invalidation tests and browser regressions for long slider gestures, Escape cancellation, and unreadable recovery protection. Existing export, crop, mask, import, navigation and recovery checks remain enabled.

The graphics stack remains Uno SDK 6.7.30 / SkiaSharp 3.119.4. No camera RAW, AI processing or feature-complete Lightroom parity is claimed.
