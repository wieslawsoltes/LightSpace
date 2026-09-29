# Validation scopes and regressions

The engine runner checks normalized models, actual rendered pixels, curve/brush/reference equivalence, schema/XMP interchange and controlled storage failures. Its performance reports are scoped microbenchmarks and work-avoidance counters, not universal application-speed claims.

Browser acceptance uses a real Uno WebAssembly publication. Tests obtain read-only arranged bounds and then send real pointer/keyboard/file-picker input. They verify model results, persisted IndexedDB records and downloaded image pixels. Production-mode checks ensure ordinary sessions work without diagnostic-tree polling.

## Reference comparison and selective settings

The reviewed PR #13 head `decf86accdadfa4b967c3232eb569e136e2cabef` passed 209 engine checks and all 41 browser tests in [Build 36621515404](https://github.com/wieslawsoltes/LightSpace/actions/runs/36621515404). The browser validation artifact SHA-256 is `feb7f6141556782680465d5bd072ec0d25ee05f9cc89cfacf824389d042791b4`. Its JUnit report has zero failures, errors or skipped tests. These are pre-merge results, not an assertion about a later deployment.

`ComparisonTransferTests` isolates all thirteen transfer groups, checks metadata retention and no-op behavior, validates missing and duplicate targets, and verifies one-transaction undo/redo. Source ownership tests cover one-slot eviction, original replacement, forced finalization and deferred SKPicture draws after the last managed source owner is released. The public six-field RendererStatistics constructor/deconstruction remains tested.

`reference-sync.spec.mjs` verifies unchanged reference pixels while active pixels change, shared source decode counts, no extra catalog photo, linked/unlinked navigation, stacked layout, selective exported settings, metadata retention, batch undo and recovery. `reference-layout.spec.mjs` checks normalized pan through resizing and bounded, content-sized dialogs. The fatal-slider test still sweeps all 17 sliders, records 187 observations, verifies one commit per gesture, and checks actual exported/reloaded values.

The paired-view engine report measures 200 warmed raster draws with no additional source decodes or shader builds. The browser report records one decoded source and two independently edited render identities; three rating changes add no decode or shader builds. Additional visible panes still issue additional draws. These results establish avoided work, not a universal speedup or a fully GPU-based import/export pipeline.

## Fresh layout versus cached diagnostics

Read-only diagnostic snapshots are cached between publications. Two identical reads of one snapshot do not demonstrate stable layout. A color-mask regression exposed this distinction: displaying the first sample swatch shifted the exposure rail by 26 pixels while the test still held its old coordinates. The recorded pointer gesture landed on the label, leaving exposure unchanged.

The color editor now reserves the swatch rail's height, so adding/removing the first sample does not move downstream adjustments. Unchanged editor values do not refresh the control tree. A regression checks rail geometry across empty/populated/empty states and verifies a real exposure gesture afterward.

Diagnostic publications carry a monotonically increasing sequence in diagnostic mode only. Pointer helpers wait for browser animation frames and equivalent bounds from distinct publications rather than comparing two reads of a stale object. Exported-image checks retain their exposure and pixel assertions; they are not skipped or relaxed to hide input failures.

## Recovery fault injection

The browser store tests remove required blobs, alter supplied bytes, introduce inconsistent lengths/identities and inject a synchronous failure after a manifest write is queued. Tests verify that publication aborts and the prior manifest remains unchanged. Explicit retry, legacy migration, source integrity, protected unreadable recovery and committed-preview isolation remain covered.

## Delivery evidence

Build validates the source before producing packages and browser assets. Pages consumes a successful trusted main-branch build, verifies the artifact's commit, checks the public `build-info.json`, then runs the browser suite against that public application. PR archives can carry a synthetic merge commit; main-branch artifacts should match the deployed main commit. Package repository metadata and artifact checksums must be checked against the producing run rather than inferred from filenames.

CI uses Chromium/SwiftShader. Desktop jobs compile Windows, Linux and macOS hosts; a queued runner is not a passed check. Physical GPU timing, native pen devices, screen readers and OS file dialogs still require target-device validation. Check the latest completed run and its artifacts rather than treating this document as a static claim that any future commit passes.
