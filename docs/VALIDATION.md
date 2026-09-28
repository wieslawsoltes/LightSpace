# Validation scopes and regressions

The engine runner checks normalized models, actual rendered pixels, curve/brush/reference equivalence, schema/XMP interchange and controlled storage failures. Its performance reports are scoped microbenchmarks and work-avoidance counters, not universal application-speed claims.

Browser acceptance uses a real Uno WebAssembly publication. Tests obtain read-only arranged bounds and then send real pointer/keyboard/file-picker input. They verify model results, persisted IndexedDB records and downloaded image pixels. Production-mode checks ensure ordinary sessions work without diagnostic-tree polling.

## Fresh layout versus cached diagnostics

Read-only diagnostic snapshots are cached between publications. Two identical reads of one snapshot do not demonstrate stable layout. A color-mask regression exposed this distinction: displaying the first sample swatch shifted the exposure rail by 26 pixels while the test still held its old coordinates. The recorded pointer gesture landed on the label, leaving exposure unchanged.

The color editor now reserves the swatch rail's height, so adding/removing the first sample does not move downstream adjustments. Unchanged editor values do not refresh the control tree. A regression checks rail geometry across empty/populated/empty states and verifies a real exposure gesture afterward.

Diagnostic publications carry a monotonically increasing sequence in diagnostic mode only. Pointer helpers wait for browser animation frames and equivalent bounds from distinct publications rather than comparing two reads of a stale object. The exported-image test retains its exposure and pixel assertions; it is not skipped or relaxed to hide the input failure.

## Recovery fault injection

The browser store tests remove required blobs, alter supplied bytes, introduce inconsistent lengths/identities and inject a synchronous failure after a manifest write is queued. Tests verify that publication aborts and the prior manifest remains unchanged. Explicit retry, legacy migration, source integrity, protected unreadable recovery and committed-preview isolation remain covered.

CI uses Chromium/SwiftShader. Desktop jobs compile Windows, Linux and macOS hosts. Physical GPU timing, native pen devices, screen readers and OS file dialogs still need target-device validation. Check the latest completed run and its artifacts rather than treating this document as a static claim that any future commit passes.
