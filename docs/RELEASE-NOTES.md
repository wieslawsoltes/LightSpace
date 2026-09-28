# LightSpace 0.4.1-alpha.1

Hotfix for fatal WebAssembly failures during repeated edit-slider changes. PR #11 isolates compiled-shader ownership from native staging cleanup, releases uniform/child inputs deterministically and simplifies the hot neutral-comparison path without disabling rendering features.

Adds forced-finalization/deferred-draw lifetime regressions and a real-pointer sweep of every development slider, including histogram updates, undo/redo, actual JPEG and catalog export, final-value verification and recovery reload. CI and public Pages validation retain one combined acceptance report. Published build information includes the version and commit.

Catalog schema 4 and recovery database version 2 are unchanged. Existing originals, edits and recovery do not need to be cleared. Reload an old or fatally terminated tab to load the patch.

See [the crash investigation and evidence](WASM-SLIDER-FIX.md). The application-level reproduction is fixed; the exact upstream runtime mechanism is not claimed proven. Physical GPU drivers and every browser engine are not certified by Chromium/SwiftShader tests. Existing feature-parity boundaries remain documented in FEATURE-COVERAGE.md.
