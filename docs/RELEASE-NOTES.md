# LightSpace 0.3.0-alpha.1

Adds arbitrary-point master/R/G/B curves with shape-preserving interpolation, cached floating-point transfer tables, pressure-aware add/erase brush masks and incremental coverage caching. New reusable Uno curve and brush controls support pointer/keyboard editing and transactional undo/cancel.

Adds reviewable XMP sidecar import/export: standard metadata, an explicitly reported Camera Raw parameter/curve subset and an optional native extension preserving complete LightSpace photo settings. Schema-1/2 catalogs migrate to schema 3; unsupported older readers reject new edits rather than dropping them.

Outstanding Actions and Playwright dependency PRs were reviewed, brought current, validated and merged. The new editing/interchange work adds actual-pixel, scalar-reference, malformed-input, cache-counter and real browser gesture tests. Source and package versions are 0.3.0-alpha.1.

This remains an early functional implementation, not full Adobe Lightroom compatibility. RAW/AI, calibrated profiles, HDR/panorama, arbitrary crop straightening, native-resolution tiled inspection, indexed storage, printing and cloud services are not included. Brush coverage is capped at 1024px long edge, image export at 8192px and 8-bit sRGB. See docs/ADVANCED-EDITING.md and docs/FEATURE-COVERAGE.md for exact semantics and limits.
