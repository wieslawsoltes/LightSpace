<div align="center">

# LightSpace

### Your photographs. Your light. Your workspace.

A local-first, non-destructive photography application built with **Uno Platform**, **C#**, and **GPU-capable Skia runtime shaders**.

[Browser application](https://wieslawsoltes.github.io/LightSpace/) · [Getting started](docs/GETTING-STARTED.md) · [Architecture](docs/ARCHITECTURE.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace brings a familiar photography workflow to a shared desktop and browser codebase: browse a catalog, select a photograph, develop its light and color, refine its composition, and export a rendered copy without altering the original.

The interface follows the desktop Lightroom editing model: a compact navigation rail, album sidebar, centered photo canvas, filmstrip, histogram, right-hand development panels, and a vertical tool rail. Its application chrome, icons, sliders, point-curve control, histogram, thumbnails, and photo interaction surface are purpose-built Uno components. Standard Uno text-input, scrolling, selection, and accessibility primitives are retained where appropriate.

> **Status: 0.1.0-alpha.1 — functional early implementation, not complete Adobe Lightroom parity.** LightSpace is an independent project, not an Adobe product. Adobe branding, assets, camera profiles, processing code, catalogs, and cloud services are not included. See the explicit [coverage and limitations](docs/FEATURE-COVERAGE.md) before evaluating it for production photography.

## A practical photography workflow

**Organize locally.** Import multiple photographs, switch between grid and detail views, browse the filmstrip, search names/captions/keywords, assign ratings and flags, and organize photo references into albums. Control-click selects multiple photographs; copied settings and synchronization operate on the selection.

**Develop non-destructively.** Adjust exposure, contrast, highlights, shadows, whites, blacks, relative white balance, vibrance, and saturation. Refine five-point curves and eight color bands, apply creative presets, adjust texture/clarity/dehaze, and add vignette or grain. Basic sharpening and spatial smoothing are available; these are not AI denoising algorithms.

**Compose and refine.** Drag crop handles or a new crop, choose common aspect ratios, rotate in quarter turns, and flip. Create radial or vertical linear-gradient masks with local exposure and saturation. Alt-click a clone source and stamp destinations. Compare the edited result with its original, pan, zoom, and return to fit.

**Keep control of the result.** Undo/redo is transaction-based, so one slider gesture is one edit. Named versions preserve different looks. Export JPEG, PNG, or WebP copies, optionally ZIP a multi-photo selection, and export a portable `.lightspace` catalog containing originals and edit settings. Recovery is saved to IndexedDB in the browser and the application-data directory on desktop.

## Shared implementation, reusable pieces

| Library | Responsibility | Targets |
| --- | --- | --- |
| `LightSpace.Core` | Development settings, crops, masks, clone spots, photo/catalog models, presets | .NET 10 |
| `LightSpace.Catalog` | Versioned serialization, validation, search, filtering, sorting, album queries | .NET 10 |
| `LightSpace.Editing` | Gesture transactions, bounded undo/redo, batch synchronization, named versions | .NET 10 |
| `LightSpace.Imaging` | Bounded decoding, EXIF orientation, sRGB conversion, procedural fallback samples | .NET 10 |
| `LightSpace.Rendering.Skia` | Runtime shader development, crop transforms, histogram, rendered export | .NET 10 |
| `LightSpace.Storage` | Platform-independent import, export, and recovery contracts | .NET 10 |
| `LightSpace.Controls` | Custom Uno controls, photo canvas, thumbnails, histogram, curves, sliders, icons | Browser / desktop |
| `LightSpace.Workbench` | Composable photography workspace, inspectors, catalog UI, commands and dialogs | Browser / desktop |

The application host supplies platform storage and startup. Engine libraries do not depend on Uno. Successful CI builds produce `.nupkg` artifacts for all eight libraries; this does **not** imply that packages have been published to NuGet.org.

## Rendering technology

The project pins **Uno SDK 6.7.30**, **.NET SDK 10.0.401**, and a matched **SkiaSharp 3.119.4** dependency family. Uno 6.7 is the stable platform release selected for this implementation. SkiaSharp is intentionally aligned with Uno's native rendering dependency rather than independently upgraded across an incompatible native ABI.

The photo viewport uses `Uno.WinUI.Graphics2DSK.SKCanvasElement`. A compiled SkSL runtime shader evaluates development settings inside the host's Skia rendering path. This avoids introducing a separate browser rendering surface or an unintegrated native GPU context. The shader can run on a GPU-backed canvas and also has a software execution path. The actual backend is selected by Uno and the device; hardware acceleration on every host is **not** guaranteed.

This release is **not** a separate WebGPU compute engine. Preview images are bounded and cached; edited thumbnails and histograms use offscreen raster surfaces, and export is currently CPU-backed. CI browser tests use Chromium/SwiftShader and are not physical-GPU performance certification. [Rendering details and tradeoffs →](docs/ARCHITECTURE.md)

## Build and run

Install the SDK pinned in `global.json`, Python 3, and the Uno desktop prerequisites for your operating system.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

# Engine and image-processing validation
dotnet run --project tests/LightSpace.Engine.Tests -c Release

# Desktop application
dotnet run --project src/LightSpace.App -f net10.0-desktop \
  -p:LightSpaceDesktopOnly=true

# Publish the actual Uno browser application
dotnet publish src/LightSpace.App -f net10.0-browserwasm -c Release \
  -o artifacts/publish -p:WasmShellWebAppBasePath=/LightSpace/
python3 scripts/collect-site.py artifacts/publish artifacts/site
python3 scripts/serve-site.py --directory artifacts/site --port 4173
```

Open `http://127.0.0.1:4173/LightSpace/`. The browser application must be served over HTTP(S), not opened as a local HTML file. It is compiled Uno/.NET WebAssembly, not a static HTML mockup or JavaScript replacement for the UI.

The asset script verifies the bundled OFL font by SHA-256 and optionally downloads demonstration photographs. Set `LIGHTSPACE_NO_DEMO_PHOTOS=1` to omit external demo photographs and use original generated landscapes. The application does not fetch demo images from third-party sites while it runs.

## Reuse the engine

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;

var photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
var catalog = new CatalogDocument { Photos = [photo], ActivePhoto = photo.Id };
var session = new EditorSession(catalog);

session.Edit("Lift the light", state => state with
{
    Develop = state.Develop with { Exposure = 0.35f, Highlights = -25, Shadows = 20 }
});

using var renderer = new PhotoRenderer();
File.WriteAllBytes("mountains-edited.jpg",
    renderer.Export(photo, SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));

session.Undo(); // Restores the edit snapshot; the original bytes were never rewritten.
```

For an embedded Uno workspace, construct `StudioView` with an `EditorSession` and an implementation of `IWorkspaceStorage`. Individual `AdjustmentSlider`, `ToneCurveView`, `HistogramView`, `PhotoViewport`, and other public controls can be used independently. [Component API and ownership →](docs/ARCHITECTURE.md#embedding-and-ownership)

## Validation and delivery

`Build` runs the deterministic engine suite, publishes WebAssembly, drives real browser controls with Playwright, verifies image export, and packages the reusable libraries. Screenshots, test reports, traces on failure, source snapshots, and packages are attached to each run.

`Desktop` compiles the same host on Windows, Linux, and macOS. Compilation does not replace device-level validation of native file pickers, accessibility, or GPU drivers.

`Pages` deploys only a successful trusted main-branch build. It verifies the artifact commit, publishes through the official Pages actions, checks the public build identity, and repeats browser acceptance tests against the deployed site.

`Release` builds library packages, a browser archive, and self-contained desktop archives for `win-x64`, `linux-x64`, and `osx-arm64`. It creates a prerelease with checksums when a version tag is pushed or a maintainer dispatches the workflow. Native signing/notarization and NuGet.org publication are not configured.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/import-fixture.png artifacts/fixtures/
npm run test:browser
```

## Formats and safety limits

Image import covers JPEG, PNG, WebP, BMP, and the first decoded frame of GIF. Sources are retained byte-for-byte. Camera RAW, HEIF/HEIC, TIFF, Lightroom `.lrcat`, Adobe presets/profiles, and XMP interchange are not implemented.

Exports are 8-bit sRGB JPEG/PNG/WebP, capped at an 8192-pixel long edge. Source EXIF/IPTC metadata is not copied to rendered exports. Import limits are 64 MiB per file, 100 megapixels decoded, and 256 MiB of encoded source data per catalog. These are safety ceilings, not recommended workloads. Catalog browsing is paged in groups of 60; this is not a production-scale indexed photo database.

## Documentation

[Getting started and shortcuts](docs/GETTING-STARTED.md) · [Architecture and component API](docs/ARCHITECTURE.md) · [Feature coverage](docs/FEATURE-COVERAGE.md) · [Release notes](docs/RELEASE-NOTES.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

## License and independence

LightSpace source is [MIT licensed](LICENSE). Uno Platform, SkiaSharp, Skia, fonts, and optional sample photographs retain their respective licenses; see [third-party notices](THIRD-PARTY-NOTICES.md). Optional Unsplash demo photographs are not relicensed as MIT.

Adobe and Lightroom are trademarks of their respective owners. LightSpace uses those names only to describe a workflow reference and compatibility boundary. It does not claim endorsement, compatibility with proprietary catalogs, or pixel-identical/feature-complete reproduction.
