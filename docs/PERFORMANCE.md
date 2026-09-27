# Performance architecture and evidence

## What is measured

The engine suite writes `artifacts/engine/performance.json`. It compares the previous serialization-based state equality with the new direct value comparison on the same normalized states, using 10,000 iterations after warm-up. The report includes elapsed CPU time and thread-local managed allocations. The same test performs 250 warm 96×64 raster redraws while only rating and caption change, and records additional image decodes and shader builds.

Browser acceptance writes `artifacts/browser-exports/metadata-performance.json`. It warms a developed photograph, then changes its rating seven times through actual Uno controls. The test verifies that card construction, library/inspector construction, decoded-image count, shader-build count and thumbnail renders do not increase when page membership remains unchanged.

These are scoped microbenchmarks and work-avoidance counters. They are **not** application startup measurements, end-to-end frame latency, physical-GPU timing, or evidence of a universal speedup. CI's browser uses Chromium/SwiftShader. Compare timing distributions on your own production devices before making hardware claims.

## Pixel-aware invalidation

`PhotoStateEquality.All` replaces JSON allocation in transaction no-op detection. `Shader` compares only processing inputs, and `Pixels` adds crop geometry. Metadata, mask names and mask identities do not invalidate identical pixels. Source byte-array identity is checked separately to prevent stale results when a catalog replaces an image under the same photo ID.

`PhotoRenderer` reuses its decoded source and runtime effect for metadata-only changes, including when `PhotoDocument.Revision` advances. A crop changes the matrix rather than rebuilding the development shader. Neutral unmasked photographs use direct image drawing. Optional point-curve, color-mixer, grading, grain and vignette calculations are bypassed when neutral.

Normalize methods retain valid array/object identities. They clone only collections requiring repairs; callers must still treat snapshot arrays and original bytes as read-only and use copy-on-write edits. This is not protection against callers mutating an array in place.

## Decode and cache budgets

The main viewport retains up to five decoded previews with a 2560-pixel long edge and a 128 MiB decoded-image budget. The renderer constructor accepts explicit preview size, image-count and byte-budget settings. Cache entries are evicted using LRU access order.

Thumbnails use a separate 384-pixel decode target, at most two retained source previews and an 8 MiB decoded-image budget. Their final rendered thumbnails are bounded to 240×160 with 128 entries. Metadata updates reuse those rendered thumbnails. These cache budgets describe retained decoded images; they do not include native codec scratch allocations, source bytes, managed objects, GPU copies or peak decode memory. Some codecs decode full resolution before resizing.

Auto tone now reads a 96×64 image (6,144 pixels), rather than allocating a managed copy of every preview pixel. It remains a simple deterministic luminance heuristic.

## Stable Uno controls

The filmstrip and photo grid retain `PhotoCard` controls while the visible photo-ID sequence remains unchanged. Their labels and selection borders are updated in place; thumbnail invalidation is limited to pixel changes. A different filter result or page legitimately rebuilds the bounded page of controls.

The album sidebar is rebuilt only when its structural/filter signature changes. Mask parameter edits keep the inspector and its pointer-captured controls alive. Mask selection or structural changes rebuild only the relevant inspector. Section expansion state is retained within the running workbench.

A reduced-resolution histogram is scheduled only for changed pixels, with a timer limiting work during continuous gestures. Recovery retains the committed-snapshot contract and debounce behavior; it is not a shader/rendering concern.

## Remaining performance boundaries

Image decode and exported image encoding are still synchronous CPU/native-code operations. Catalog recovery still serializes retained originals into one JSON record. Browser/native images are not tiled, and source-pixel zoom does not recover details absent from the bounded preview. The catalog is paged in groups of 60 rather than using a durable indexed photo database.

Texture taps and grain coordinates are now expressed relative to source pixels. Downsampling still changes available spatial information, so preview and full-resolution export are not guaranteed to match exactly for fine-detail effects. A multi-resolution tiled graph, asynchronous decode scheduler, incremental original storage, physical-device profiling and true native-resolution inspection remain important follow-on work.
