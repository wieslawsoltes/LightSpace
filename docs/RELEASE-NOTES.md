# LightSpace 0.2.0-alpha.1

Four-way color grading with a custom interactive wheel; rotatable and directly editable linear/radial gradients; luminance-range masks and spatial/range intersection; local contrast/white balance, opacity and mask enable/disable; mask rename/duplicate/delete; selected-mask coverage overlay; draggable before/after comparison.

Performance work includes semantic state comparisons instead of JSON serialization, pixel-aware shader/thumbnail invalidation, neutral processing bypasses, independently budgeted 384-pixel thumbnail decoding, bounded auto-tone sampling, and stable catalog controls for unchanged page membership. Tests report managed allocations, scoped CPU timings and work counters rather than claiming physical-GPU frame rates.

Schema-1 catalogs migrate on import. New catalog saves use schema2 to preserve grading and extended masks and prevent older builds from silently dropping those settings. Keep a pre-upgrade backup when needed.

The prior committed-revision recovery guarantees remain. RAW, AI tools, calibrated lens corrections, HDR/panorama merge, native-resolution tiled inspection, Adobe catalog compatibility and complete Lightroom parity remain outside this release. See docs/FEATURE-COVERAGE.md and docs/PERFORMANCE.md.
