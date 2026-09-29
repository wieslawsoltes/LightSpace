# Performance architecture and evidence

## Evidence, not universal frame-rate claims

The engine runner emits scoped reports in `artifacts/engine`; browser tests emit real-input counters in `artifacts/browser-exports`. CPU timings, native/raster execution and avoided-work counts are identified separately. CI uses Chromium/SwiftShader and is not a physical-GPU benchmark. Stable linear-memory capacity does not prove that every allocation is leak-free.

`performance.json` compares serialization-based equality with direct normalized-value comparisons over 10,000 warm iterations, including thread-local managed allocations. It also checks that 250 warm metadata redraws do not trigger new decodes or development shader builds.

`advanced-performance.json` performs 100 warm exposure updates with a painted mask and non-neutral curve on a raster canvas. It asserts no additional brush-dab rasterization, brush texture publication or curve-table builds. Browser `metadata-performance.json` and `brush-performance.json` test the corresponding behavior through actual rating/local-exposure gestures.

`recovery-performance.json` measures 20 warm metadata commits after an 8 MiB original is stored by an in-memory adapter. The browser report separately records real IndexedDB counters. Unchanged originals are not rehashed, rewritten or read as values, but metadata writes and source-key existence checks still occur. This is not zero storage I/O or a disk-latency benchmark.

`geometry-performance.json` performs 100 warm geometry updates and raster draws at 96×64 with development and optics enabled. It records source decodes, development/presentation builds, matrix builds, elapsed CPU time and managed allocation. The invariant is that geometry updates rebuild matrices, not the other shader stages, curve tables or brush coverage. Browser photography tests repeat the work-avoidance assertion through three actual geometry-slider gestures with histogram updates.

Raw timing varies with the runner and active state. Use the reports attached to the exact commit under evaluation, and profile target-device distributions before promising latency or frame rates.

## Independent render stages

`PhotoStateEquality.Shader` compares development, masks and clone inputs. `Pixels` additionally includes crop, geometry and optics. `All` includes metadata and catalog-only distinctions. Normalized direct comparison avoids JSON allocation and lets metadata edits retain pixel caches.

The main development shader feeds a presentation runtime effect for manual distortion, channel alignment, lens falloff and display-only clipping. That stage directly evaluates its child; it does not create a CPU intermediate image between display effects. Geometry is a separate draw matrix. A geometry-only change therefore retains both shader stages. A separate analysis cache keeps histogram refreshes from invalidating the viewport's optical/clipping variant.

Chromatic alignment can require three development-child evaluations per output point. It is not automatically cheaper than every alternative for a complex mask/detail stack. We retain the single composed graph here to avoid adding an intermediate render target and synchronization/readback path; target-device profiling is still required for a future tiled/multi-pass engine.

Neutral development uses direct image drawing. Neutral presentation bypasses its wrapper. Optional neutral curve, mixer, grading, grain and vignette work is bypassed in the development effect. Source identity prevents stale reuse after opening another catalog with the same photo ID.

The `RuntimeShaderScope` lifecycle repair is preserved for new composition: compiled output stays field-rooted through staging cleanup, borrowed children are not accidentally disposed, and ownership transfers only afterward. Native finalization/deferred-draw tests and the full browser slider stress guard this path.

## Projective and pointer mapping

Projective framing is solved in double precision. Constrain crop brackets and binary-searches a conservative covered frame; it does not perform a per-pixel CPU remap. The result becomes a Skia draw matrix. Weakly keyed projection/inverse caches avoid recomputing that solve for each point in a mask outline or brush cursor.

Optical source/display inversion is analytic. Color picking, brush input and white balance reverse crop/orientation, geometry and optics without reading the rendered canvas. A color or white-balance pick reads only a bounded original-preview patch—25 pixels at the default interior footprint—not a managed copy of the entire image.

## Curves and brush coverage

Curve compilation produces a 2048-entry RGBA-float image. Up to eight semantically keyed lookup tables are retained. Only curve edits rebuild them; unrelated tone, metadata, optics and geometry reuse the table. Finite lookup sampling and 8-bit exports remain precision boundaries.

Brush coverage is represented as `multiplier * analyticCoverage + bias`. Floating-point stroke accumulation recognizes appended dabs and updates bounded raster regions. Local exposure, color ranges and geometry do not replay unchanged dabs. Undo or edits to earlier strokes cause replay. Texture publication still rebuilds an immutable coefficient image rather than issuing sparse GPU subregion updates.

Brush coverage is capped at a 1024-pixel long edge, including export. Per mask: 64 strokes, 4096 dabs per stroke, 65,536 total dabs, and a 200-million visited-pixel replay safety limit. The retained brush cache budget is separate from decoded-image budgets; it excludes transient arrays and GPU copies.

## Decode and retained memory

Viewport decoding targets a 2560-pixel long edge, five images and a 128 MiB retained decoded-image budget. Thumbnail decoding targets 384 pixels, two sources and 8 MiB; rendered thumbnails are at most 240×160 with up to 128 entries. Some codecs decode larger scratch buffers before resizing, so these are retention limits, not total peak-allocation guarantees.

Histogram analysis remains a throttled 192×128 raster sample. Auto tone reads 96×64 pixels instead of copying a full preview. New histogram interaction and clipping visualization do not turn analysis into a GPU compute reduction. Export decodes retained originals and encodes an 8-bit sRGB raster, with an 8192-pixel long-edge cap.

## UI reuse and diagnostics

Photo cards survive metadata edits while page membership is unchanged. Filter/page changes may rebuild the bounded page. Sidebar reconstruction follows structure/filter changes; captured editor controls remain alive through continuous gestures. Histogram changes do not replace the pointer-captured histogram itself.

Panel resizing uses coordinates relative to a stable parent, avoiding feedback as the grip moves. Layout visibility/width changes do not create photo transactions. Session-only section expansion and panel layout remain separate from photographic state.

Diagnostic registrations are weak. Brush coordinate arrays are omitted, and normal sessions do not subscribe to periodic UI-tree serialization. Test helpers require fresh, stable arranged bounds before input. Reports distinguish actual constructed resources and completed operations from GPU timing, which is not inferred from these counters.

## Remaining performance work

Decode, image encoding, first-use source hashing, manifest serialization, brush texture preparation and some native resource creation remain synchronous. Recovery startup loads referenced originals; it does not page them on demand. Display sources are not tiled. Source-pixel zoom describes geometry but cannot reveal information missing from a bounded preview.

The next large architectural steps are tiled native-resolution source/render storage, asynchronous decode/export scheduling, source compaction/paging, GPU analysis reductions and measured multi-pass tradeoffs on real devices. None is represented as complete by these work-avoidance tests. See [feature boundaries](FEATURE-COVERAGE.md) and [optical/geometry semantics](OPTICS-GEOMETRY.md).
