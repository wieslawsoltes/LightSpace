<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · GPU-capable Skia effects · Reusable C# libraries

[Open LightSpace](https://wieslawsoltes.github.io/LightSpace/) · [Guide](docs/GETTING-STARTED.md) · [Advanced editing & XMP](docs/ADVANCED-EDITING.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace is a shared Uno desktop/WebAssembly photography application: browse a local catalog, develop a photograph, refine its composition and masks, and export a rendered copy while retaining the original bytes. The dark workspace combines a library sidebar, centered photo canvas, filmstrip, histogram, development inspector and vertical tool rail.

**Current version: 0.3.0-alpha.1.** This is independent, functional early-stage software—not a pixel-identical or feature-complete Adobe Lightroom replacement. No Adobe artwork, camera profiles, proprietary processing code or cloud services are included. [Implemented behavior and remaining boundaries →](docs/FEATURE-COVERAGE.md)

## New in 0.3

**Arbitrary RGB curves.** Edit master, red, green and blue curves with up to 32 points each. Add/drag/delete points, enter numeric values, choose linear or shape-preserving smooth interpolation, and undo each gesture. A separate floating-point lookup cache avoids rebuilding curves when unrelated adjustment values change.

**Freehand masks.** Paint and erase with size, feather, flow, density and pen-pressure inputs. Arc-length resampling avoids pointer-event-dependent stroke density. Add/subtract brush coverage from existing analytic masks or create a new brush mask. Incremental coverage caching processes appended dabs rather than replaying the whole stroke on every update. Local adjustment changes reuse coverage.

**XMP sidecars.** Import standard metadata and an explicit Camera Raw parameter/curve subset with a compatibility report before applying it. Metadata-only import preserves processing. Export standard metadata, supported development values and an optional native extension for complete LightSpace settings round trips. Unknown Adobe processing is reported, not claimed as equivalent.

**Reliable delivery.** The outstanding Actions and Playwright updates were validated and merged. New engine/browser tests cover curve interpolation and pixels, brush replay equivalence, XMP parsing and round trips, gesture cancellation, schema migration and cache work counters. [Advanced editing semantics and safety limits →](docs/ADVANCED-EDITING.md)

## Photography workflow

**Organize.** Import JPEG, PNG, WebP, BMP or GIF sources. Browse the paged grid/filmstrip, search names/captions/keywords, assign ratings and flags, and collect references in albums. Control-click extends the selection. Copy/paste and synchronization apply development settings across selected photographs.

**Develop.** Adjust light, relative white balance, vibrance/saturation, monochrome, curves and eight color bands. Four-way grading controls shadows, midtones, highlights and global color with blending and balance. Creative presets, texture/clarity/dehaze approximations, sharpening, spatial smoothing, vignette and grain provide additional tools.

**Refine.** Drag crop rectangles and handles, choose centered ratios, rotate in quarter turns and flip. Work with radial/linear gradients, luminance ranges and brush masks. Local exposure, contrast, temperature, tint and saturation remain non-destructive. Clone stamps use an explicit source; comparison has a draggable divider.

**Preserve.** Each completed gesture is one transaction. Undo/redo, named versions, selected-photo synchronization, catalog backups and recovery preserve the settings. Export JPEG/PNG/WebP copies, ZIP a selection or create an XMP sidecar. Recovery acknowledges committed revisions only and protects unreadable prior data from automatic replacement.

## Eight reusable libraries

| Library | Responsibility | Target |
| --- | --- | --- |
| `LightSpace.Core` | Photo state, grading, curves/interpolation, masks/brush samples, geometry and presets | .NET 10 |
| `LightSpace.Catalog` | Schema migration, serialization, queries, XMP metadata/settings interchange | .NET 10 |
| `LightSpace.Editing` | Gesture transactions, bounded undo/redo, versions, synchronization, revision-aware recovery | .NET 10 |
| `LightSpace.Imaging` | Bounded decoding, EXIF orientation, sRGB conversion, fallback artwork | .NET 10 |
| `LightSpace.Rendering.Skia` | Runtime effects, decoded-image cache, floating-point curve tables, incremental brush coverage, histogram/export | .NET 10 |
| `LightSpace.Storage` | Import/export/recovery contracts and optional sidecar-picker capability | .NET 10 |
| `LightSpace.Controls` | Original Uno chrome/icons, sliders, cards, grading/mixer/curve/mask/brush controls and photo canvas | Uno browser / desktop |
| `LightSpace.Workbench` | Composable workspace, inspectors, commands, dialogs and save UX | Uno browser / desktop |

Engine libraries do not depend on Uno. The host supplies platform storage and startup. Standard Uno text-input, scrolling, selection and accessibility primitives remain where appropriate; not every primitive is a newly implemented control. Successful Build runs produce eight `.nupkg` artifacts, not an implicit NuGet.org publication.

## Stack and rendering

Pinned versions are **.NET SDK 10.0.401**, **Uno SDK 6.7.30** and matched **SkiaSharp 3.119.4** managed/native assets. Graphics dependencies must be upgraded together and validated against Uno's ABI.

The viewport uses `Uno.WinUI.Graphics2DSK.SKCanvasElement` and compiled SkSL runtime effects within Uno's Skia composition path. Hardware execution depends on the host supplying a GPU-backed canvas. Software rendering remains available for deterministic tests and export. This release does **not** have a separate WebGPU compute backend.

CPU/native decode, recovery, brush texture publication and export remain synchronous. Viewport sources have a 2560-pixel preview target; thumbnails decode at 384 pixels; brush coverage is capped at 1024 pixels even during export. Curves use 2048-entry floating-point lookup images. Cache budgets bound retained buffers, not all transient allocations or GPU copies. [Architecture](docs/ARCHITECTURE.md) · [Performance methods and evidence](docs/PERFORMANCE.md)

## Build and run

Install the SDK in `global.json`, Python 3 and your operating system's Uno desktop prerequisites.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

# Model, processing, recovery, XMP and performance checks
dotnet run --project tests/LightSpace.Engine.Tests -c Release

# Shared native desktop host
dotnet run --project src/LightSpace.App -f net10.0-desktop \
  -p:LightSpaceDesktopOnly=true

# Actual Uno WebAssembly publication
dotnet publish src/LightSpace.App -f net10.0-browserwasm -c Release \
  -o artifacts/publish -p:WasmShellWebAppBasePath=/LightSpace/
python3 scripts/collect-site.py artifacts/publish artifacts/site
python3 scripts/serve-site.py --directory artifacts/site --port 4173
```

Open `http://127.0.0.1:4173/LightSpace/`. Serve the application over HTTP(S); it is compiled Uno/.NET WebAssembly, not an HTML mockup. The asset script verifies the OFL font and optionally downloads demonstration photos at build time. Set `LIGHTSPACE_NO_DEMO_PHOTOS=1` to use original generated landscapes instead.

## Embed processing or controls

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;

var photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
var session = new EditorSession(new CatalogDocument { Photos = [photo], ActivePhoto = photo.Id });
session.Edit("Refine contrast", state => state with
{
    Develop = state.Develop with
    {
        Exposure = .3f,
        Channels = new ChannelCurves
        {
            Master = new PointCurve { Points = [new(0, 0), new(.25f, .18f), new(.75f, .82f), new(1, 1)] }
        }
    }
});
using var renderer = new PhotoRenderer();
File.WriteAllBytes("mountains-edited.jpg",
    renderer.Export(photo, SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));
session.Undo(); // Original encoded bytes are untouched.
```

Embed `StudioView(session, storage, recoveryLoaded)` or individual public controls. `PointCurveEditor`, `ColorGradingEditor` and `MaskSettingsEditor` expose preview/commit/cancel contracts without owning the host's transaction policy. `BrushSettingsEditor` changes tool settings, not existing strokes. `ISidecarStorage` is optional for embedded hosts.

Dispose workspaces and native-resource caches. Use copy-on-write arrays for snapshots and retain original bytes read-only; mutation in place violates the cache contract. Objects are confined to one logical owner, normally the UI synchronization context. [Component API](docs/ADVANCED-EDITING.md) · [Recovery invariants](docs/RECOVERY.md)

## Tests and workflows

`Build` runs the engine suite, publishes WebAssembly, exercises actual Uno controls with Playwright, verifies exports/recovery and packages all libraries. Reports include screenshots, failure traces, machine-readable test results and performance counters. `Desktop` compiles Windows, Linux and macOS hosts. `Pages` deploys a successful trusted main build, checks commit provenance and repeats acceptance tests against the public URL. `Release` packages libraries and browser/native archives with checksums.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/import-fixture.png artifacts/fixtures/
npm run test:browser
```

CI browser testing uses Chromium/SwiftShader. It is not physical-GPU or pen-hardware certification. Microbenchmarks and work-avoidance counters are scoped evidence, not claims of universal application speedup. Native signing/notarization, installers, NuGet.org publication and automatic updates are not configured.

## Compatibility and data safety

Catalog schema **3** preserves curves and brush data. Schemas 1 and 2 migrate with neutral defaults; older applications reject unsupported new files. Keep pre-upgrade backups when old-version interoperability matters.

Import limits are 64 MiB per source, 100 megapixels decoded and 256 MiB of encoded originals per catalog. Image output is 8-bit sRGB with an 8192-pixel long-edge cap; source EXIF/IPTC is not embedded in rendered copies. XMP supports a documented subset, not Lightroom catalogs/profiles or pixel-equivalent Adobe development.

RAW/DNG/HEIF/TIFF, AI tools, calibrated camera/lens correction, HDR/panorama merging, arbitrary crop straightening, native-resolution tiled inspection, indexed durable catalog storage, printing/proofing and cloud synchronization remain unimplemented. Recovery is an unencrypted local JSON record, not a cross-tab merge system or a substitute for original-file backups. [Complete boundary ledger](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

LightSpace source is [MIT licensed](LICENSE). Dependencies, the OFL font and optional Unsplash photographs retain their licenses; photos are not relicensed as MIT. Adobe and Lightroom are trademarks of their owners and are used only as comparative workflow references. No endorsement or proprietary catalog compatibility is claimed.
