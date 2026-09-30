# LightSpace 0.7.0-alpha.1

## Multi-photo Survey culling

Open Survey from the left rail or with N. Selected candidates (or the current filter when fewer than two are selected) are shown in aspect-aware rows, twelve per page. Use individual rating/flag controls or 0–5 and P/X/U. Exclude candidates without deleting catalog photos or originals, hide rejected candidates, and restore excluded photos. Enter/double-click opens Detail; arrows and page keys navigate candidates.

Metadata edits deliberately target only the active candidate while retaining catalog multi-selection. They use normal atomic undo and committed-revision recovery. Exclusion, page and active-navigation state are view-only and session-local.

## Rendering and ownership

One Uno Skia surface draws photos directly through the composed development/optics/geometry pipeline. A separate survey renderer has twelve processing identities, a 1024px preview target and 48 MiB retained decoded-source budget. It prepares at most one candidate per dispatcher tick and reuses cards/layout/pixel caches for metadata changes. Leaving Survey clears its preview caches and source references.

Preparation still includes synchronous CPU/native decoding and brush work; it is not GPU decoding, a background worker, or a guaranteed frame time. The decoded budget excludes source bytes, codec scratch memory, brush/curve caches, other renderers and GPU allocations. GPU execution remains dependent on the Uno host's Skia backend.

## Validation and compatibility

Engine and browser regressions cover candidate paging and order, mixed-aspect layout bounds, reversible exclusions, single-target edits and undo, original-byte retention, warm twelve-source rendering, keyboard navigation, resizing and recovery. Existing optics, masks, reference, XMP, publication-safety and all-slider fatal-crash regressions remain enabled. Use the commit-specific Build, Desktop and Pages reports for exact verified results.

Catalog schema 5, native XMP settings 5, recovery manifest 1 and IndexedDB version 2 are unchanged. Reload an old tab to load new code; do not clear site data. Survey candidates/exclusions are not persisted. SourceLink, symbols, six-RID single-file release packaging and Trusted Publishing workflows are unchanged; source version bumps and artifacts do not themselves publish a package or release tag.

This closes a Survey workflow gap, not all Lightroom parity. Survey is fit-only, with bounded preview resolution. RAW/AI, calibrated profiles, automatic alignment/Upright, HDR/panorama, native-resolution tiling, virtual copies, print/proofing and cloud workflows remain absent. See [Survey](SURVEY.md), [performance](PERFORMANCE.md) and [feature coverage](FEATURE-COVERAGE.md).
