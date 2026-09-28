# Performance architecture and evidence

## Measured scope

The engine suite emits `artifacts/engine/performance.json`, comparing serialization-based equality with direct value comparison on the same normalized states for 10,000 warmed iterations. It reports elapsed CPU time and thread-local managed allocations. A separate 250-iteration warm metadata-redraw check records additional image decodes and shader builds.

`artifacts/engine/advanced-performance.json` measures 100 warm exposure updates with both a painted mask and non-neutral RGB curve on a raster canvas. It verifies that those updates do not rasterize additional brush dabs, publish new brush textures or rebuild curve tables. The elapsed time is CPU/raster execution, not GPU frame latency.

Browser acceptance emits `artifacts/browser-exports/metadata-performance.json` for seven real rating changes and `brush-performance.json` for three local-exposure gestures after painting. Counters verify stable card/library/inspector construction and reuse of decoded images, shaders, thumbnails, brush coverage and curve tables where applicable.

These are scoped microbenchmarks and work-avoidance assertions. They are not application startup measurements, physical-GPU timings, pen-hardware certification or universal speedup claims. CI uses Chromium/SwiftShader. Compare timing distributions on target devices before making hardware claims.

## Pixel-aware invalidation

`PhotoStateEquality.All` compares normalized values without serializing JSON. `Shader` compares only processing inputs; `Pixels` adds crop geometry. Ratings, captions, mask names and mask IDs do not change identical pixel results. Original byte-array identity prevents reuse of a different source under the same photo ID.

Metadata edits reuse decoded sources, runtime effects and thumbnails even if document revisions advance. Crop changes affect the matrix instead of rebuilding the development shader. Neutral photographs use direct image drawing. Neutral optional curve, mixer, grading, grain and vignette calculations are bypassed.

Normalize methods retain valid object/array identities and copy only collections requiring repair. Callers must not mutate retained snapshot arrays in place. Brush-dab normalization is memoized by immutable array identity. This is an ownership contract, not a mechanism that detects arbitrary external mutation.

## Curve tables

`ToneLookupCache` compiles master/channel curves into a 2048-entry RGBA-float image and retains up to eight semantically keyed tables. A curve edit rebuilds a table; exposure, mask adjustments, metadata and crop changes reuse it. Shape-preserving interpolation is compiled once per point set; evaluation is allocation-free. Sampling a finite table is an approximation of the analytic curve, while export remains 8-bit sRGB.

## Incremental brush coverage

The cache stores the affine transform `coverage = multiplier * analyticCoverage + bias`. It retains floating-point stroke accumulation and recognizes append-only dab growth. New dabs update only their bounded raster region; changing local exposure or luminance restriction does not replay geometry. Undo or edits to an earlier stroke trigger replay.

Texture publication currently rebuilds an immutable 8-bit coefficient image after coverage changes. Thus a stroke does not rerasterize all previous dabs on each update, but publication is not a sparse GPU-texture upload. Coverage has a 1024-pixel maximum long edge, including export. This precision/quality boundary is explicit rather than described as native-resolution processing.

Per-mask limits are 64 strokes and 65,536 dabs, with 4096 dabs per stroke. Replay aborts after 200 million visited pixels. Retained brush caches default to 128 MiB, independently of decoded-image caches. These budgets exclude temporary snapshot arrays, codec scratch memory, source buffers and GPU copies; they are not total-process peak-memory limits.

## Decode and image-cache budgets

The viewport targets a 2560-pixel long edge, up to five retained source previews and a 128 MiB decoded-image budget. Thumbnails use a 384-pixel target, at most two retained decoded sources and an 8 MiB budget. Their rendered images are at most 240×160, with 128 cached thumbnails. Codec implementations may decode full-resolution data before resizing, so retained budgets do not limit every transient decode allocation.

Auto tone reads a 96×64 image (6,144 pixels), rather than allocating a managed copy of the whole preview. It remains a simple deterministic luminance heuristic.

## Stable Uno controls and diagnostics

Filmstrip/grid `PhotoCard` instances survive metadata updates while page membership is unchanged. Labels and selection state update in place, and only pixel edits invalidate thumbnails. Changes to filters or page membership legitimately rebuild the bounded page.

Sidebar construction follows structural/filter changes. Continuous mask and curve editing retains captured controls; selection or structural changes rebuild the appropriate inspector. Section expansion survives reconstruction within the session. Histograms are reduced-resolution, scheduled only for pixel changes and throttled during continuous gestures.

Diagnostic registrations use weak references, avoiding ownership of discarded controls. Large brush coordinate arrays are excluded from diagnostic serialization; brush summaries contain IDs and counts only. Normal sessions do not subscribe to periodic diagnostic-tree snapshots. Test counters represent completed operations, not an inferred GPU timeline.

## Remaining performance boundaries

Decode, image encoding, brush texture publication and catalog serialization still perform synchronous CPU/native work. Recovery now serializes a source-free metadata manifest on the incremental store path. First-use source hashing/staging and restore still process originals; the legacy embedded-host snapshot writer still includes them. Photos are not tiled; source-pixel zoom cannot reveal information absent from the bounded preview. Browsing uses 60-photo pages rather than an indexed durable catalog.

Source-relative detail/grain coordinates improve consistency, but downsampling changes available information and brush rasterization is bounded, so preview/full-resolution export are not guaranteed identical for fine details. Native-resolution tiled processing, asynchronous decode/export scheduling, source paging/compaction and target-device profiling remain separate workstreams.

## Source-separated recovery and bounded color sampling (0.4)

`RecoveryPersistence` weakly memoizes encoded-original SHA-256 keys by immutable array identity. After a successful initial write, a metadata-only capture contains source references and metadata but no encoded-original payload. Known sources are not hashed or resent. A failed commit clears the known-key set so retry can restage missing data; restore validates hashes and primes the warm cache. Native commits still check source-file existence, and browser commits perform key-only IndexedDB requests. This is avoided original-byte work, not zero storage I/O.

The engine report `recovery-performance.json` measures 20 warm metadata commits with one 8 MiB source using an in-memory store. It includes additional bytes hashed, blob writes, manifest bytes, CPU elapsed time and thread-local managed allocations. Browser acceptance emits its own `browser-exports/recovery-performance.json` for real rating edits and real IndexedDB transactions, including original-value reads and bytes written. Timings from the in-memory test are not claims about native disks or browser persistence latency.

Source color picking reads a maximum 5×5 patch at the default radius, not `bitmap.Pixels` for a full decoded image. The public sampler permits radius 0–8 and counts the actual patch pixels. It reads the retained preview before development, avoiding full-original decode for each click. Color-range samples are transformed to Oklab when uniforms are prepared; the shader converts source color once per pixel only when a range is active. Masks retain existing brush/curve cache separation.

Manifest metadata still scales with photo, stroke and version counts. Startup still restores all referenced originals. First writes can have substantial interop and encoding allocation, and this release does not implement source paging, automatic orphan cleanup, texture tiling or a transactional indexed catalog database.
