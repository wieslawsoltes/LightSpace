# WebAssembly edit-slider crash: 0.4.1 hotfix

## Report and reproduction

The reported failure is a fatal `RuntimeError: memory access out of bounds`, followed by repeated `.NET runtime already exited with 1` assertions from later pointer callbacks. The repeated runtime-exit messages are aftermath, not separate slider exceptions. Switching the application's main-entry API or suppressing console errors would not repair the invalid access.

The retained diagnostic run [36417187494](https://github.com/wieslawsoltes/LightSpace/actions/runs/36417187494) exercised the immutable 0.4.0 browser artifact at `fe1df0bb9ffdd99a8e05abe68f9a51b582ccbfd7`. Chromium crashed during the first Exposure sweep, before any of the 17 sweeps completed. The original diagnostic wrapper retained the failing test's exit code in its artifact, despite a successful workflow conclusion. Later revisions of that diagnostic workflow propagate failures directly.

Merely adding deterministic uniform disposal was insufficient: [build 36419148047](https://github.com/wieslawsoltes/LightSpace/actions/runs/36419148047) still failed during Exposure. The final implementation at `d8f10df48314195ef8465f9a0ccd8e7760660792` passed the same full stress path in [build 36425510483](https://github.com/wieslawsoltes/LightSpace/actions/runs/36425510483), followed by the existing 28 acceptance tests. PR #11 merges that validated change.

## Repair

`RuntimeShaderScope` gives each compilation explicit ownership of uniform staging data, child shader wrappers, and the compiled shader. The compiled result is retained in a field while staging resources are released, and ownership is transferred to the renderer only after cleanup. Failure disposes the untransferred output and inputs. Source-image lifetime is kept explicit across compilation.

Uniform population is separated from compilation/ownership rather than keeping the whole path in one large nested cleanup scope. Neutral-state mixer comparison reads normalized scalar leaves instead of generic reference-record equality dispatch. Rendering still uses the existing Skia effect; no slider, mask, effect, histogram, export, SIMD/JIT capability or fatal-error reporting is disabled.

The source-level lifecycle/execution-shape change eliminates the retained application reproduction. The precise Mono/Jiterpreter/compiler mechanism has not been reduced to a standalone upstream reproducer, so the evidence does not establish a particular upstream compiler defect. Returning a managed value through `finally` is not generally invalid C#; this repair deliberately uses a simpler explicit ownership shape for the affected WebAssembly path.

The pinned stack remains .NET SDK 10.0.401, Uno SDK 6.7.30 and matched SkiaSharp 3.119.4. Catalog schema 4 and IndexedDB version 2 are unchanged. No recovery reset, source deletion or migration is required by this patch.

## Regression checks

`RendererLifetimeTests` forces collection/finalization across repeated shader replacements and neutral transitions, replays recorded SKPictures after disposing renderer caches, and verifies that separately created shaders retain independent uniform snapshots.

`slider-stability.spec.mjs` drives all 17 actual Uno development sliders across ten destinations each, including both extremes and neutral crossings: Exposure, Contrast, Highlights, Shadows, Whites, Blacks, Temperature, Tint, Vibrance, Saturation, Texture, Clarity, Dehaze, Vignette, Grain, Sharpening and Noise reduction. It records 187 observations, catches the first fatal console/page/process error, and verifies each complete gesture commits once.

The test also exercises histogram redraws, five undo/redo cycles, decodes a real exported JPEG, exports a portable catalog, checks the actual final value of every slider, reloads recovery, and verifies those values again. No testing-only state mutation replaces pointer input.

Build and Pages now run the suite in a single Playwright invocation, preserving the stress test in the same JUnit/HTML report rather than overwriting its report with a second invocation. `slider-stability.json`, the JPEG and both catalogs are retained as evidence. Pages verifies the deployed commit before rerunning the suite against the public site.

The pre-merge passing sweep retained a 258,146,304-byte WebAssembly linear-memory capacity across its 187 observations. That is capacity, not live allocated memory: stability does not establish absence of every leak. Tests use Chromium/SwiftShader; they do not certify physical GPU drivers, every browser engine or arbitrary oversized imported catalogs.

## Loading the fix

Reload the application after deployment. If a tab was already fatally terminated, it must be reloaded; subsequent pointer events cannot revive that runtime. Use a hard refresh if an older entry document remains cached. Do not clear site data or IndexedDB as a troubleshooting step: those stores contain local recovery.

`build-info.json` records both the application version and commit so the deployed hotfix can be distinguished from a stale tab. Only changes acknowledged by recovery before a fatal crash can be restored; this fix cannot reconstruct an unsaved preview lost in the earlier crash.
