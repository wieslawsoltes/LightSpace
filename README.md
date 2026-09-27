<div align="center">

# LightSpace

### Develop your photographs. Keep your originals.

A local-first photography workspace for **Uno Platform**, powered by reusable C# libraries and GPU-capable **Skia runtime effects**.

[Open in your browser](https://wieslawsoltes.github.io/LightSpace/) · [Getting started](docs/GETTING-STARTED.md) · [Architecture](docs/ARCHITECTURE.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace combines a familiar photography layout with a shared native desktop and WebAssembly implementation: library navigation, a centered photo canvas, filmstrip, histogram, development inspector and vertical tool rail. Original bytes remain unchanged while edits, masks, metadata and alternative looks are recorded in the local catalog.

**Current version: 0.2.0-alpha.1.** This is an independent functional early implementation, not a pixel-identical or feature-complete Lightroom replacement. It includes no Adobe branding, proprietary processing code, camera profiles or cloud services. [Implemented features and remaining boundaries →](docs/FEATURE-COVERAGE.md)

## New in 0.2

**Four-way color grading.** Shadows, midtones, highlights and global grading have an original interactive hue/saturation wheel, numeric controls, luminance, blending and balance. Grading participates in undo/redo, copy/sync, presets, versions, export and recovery. Two new grading presets provide starting points.

**Directly editable masks.** Linear gradients follow the drag direction and have live position/fade/rotation handles. Radial gradients can move, resize and rotate on the photo. Luminance-range masks select source brightness; spatial gradients can intersect with the same range. Masks support local exposure, contrast, relative white balance and saturation, opacity, disable, inversion, rename, duplicate and delete. Weighted coverage can be inspected without contaminating exported pixels.

**Less redundant work.** Metadata updates no longer rebuild identical photo shaders or thumbnails. Catalog cards and library controls remain alive when page membership is unchanged. Thumbnails use a separate 384-pixel decode target and bounded cache, Auto tone samples only 6,144 pixels, and transaction comparisons no longer serialize JSON. The before/after divider is now draggable without changing the document revision.

[Color and mask guide](docs/COLOR-AND-MASKS.md) · [Performance methodology](docs/PERFORMANCE.md) · [Changelog](CHANGELOG.md)

## Photography workflow

**Organize locally.** Import JPEG, PNG, WebP, BMP or GIF photographs. Browse a paged grid or filmstrip, search filenames/captions/keywords, assign ratings and flags, and collect photo references in albums. Control-click extends a selection for rating, copying or synchronizing edits.

**Develop non-destructively.** Adjust light, relative white balance, vibrance and saturation; edit five-point curves and eight color bands; grade tonal ranges; apply creative presets; adjust texture, clarity, dehaze, vignette, grain, sharpening and spatial smoothing. These are original algorithms and approximations, not Adobe processing or AI denoising.

**Refine composition and local light.** Drag crop rectangles and handles, apply centered ratios, rotate by quarter turns and flip. Use radial, linear or luminance masks. Alt-click a clone source and stamp destinations. Pan, zoom, fit, show the original and drag the before/after divider.

**Preserve the result.** Each gesture is one undo transaction. Named versions retain alternative looks. Export JPEG/PNG/WebP copies, ZIP a multi-photo selection, or save a portable `.lightspace` catalog with originals and edits. Recovery tracks completed committed revisions, excludes active previews and offers explicit retry. Unreadable recovery is protected until the user confirms replacement.

## Reusable libraries

| Library | Responsibility | Targets |
| --- | --- | --- |
| `LightSpace.Core` | Development/grading, geometry, masks, model values, semantic equality and presets | .NET 10 |
| `LightSpace.Catalog` | Versioned serialization/migration, validation, search, filtering and album queries | .NET 10 |
| `LightSpace.Editing` | Gesture transactions, undo/redo, batch synchronization, named versions, revision-aware recovery | .NET 10 |
| `LightSpace.Imaging` | Bounded decoding, EXIF orientation, sRGB conversion and fallback sample generation | .NET 10 |
| `LightSpace.Rendering.Skia` | Runtime-effect processing, byte-budgeted image caches, transforms, histogram and export | .NET 10 |
| `LightSpace.Storage` | Platform-independent import/export/recovery contracts | .NET 10 |
| `LightSpace.Controls` | Custom chrome/icons, sliders, grading wheel/editor, mixer, mask editor, cards, curves, histogram and viewport | Uno browser / desktop |
| `LightSpace.Workbench` | Composable photography shell, inspectors, commands, dialogs and recovery UX | Uno browser / desktop |

Engine libraries do not depend on Uno. The application supplies startup and platform storage. All eight libraries are independently packable; successful CI builds attach `.nupkg` artifacts. They are not automatically published to NuGet.org.

The application uses original custom photography controls while retaining standard Uno text input, scrolling, selection and accessibility primitives where appropriate. Not every framework primitive has been reimplemented.

## Rendering and pinned stack

The repository pins **.NET SDK 10.0.401**, **Uno SDK 6.7.30** and the matched **SkiaSharp 3.119.4** dependency family. Managed/native graphics packages must be upgraded together and validated against Uno's hosting ABI.

The viewport renders through `Uno.WinUI.Graphics2DSK.SKCanvasElement`. A compiled SkSL runtime effect performs photographic development inside Uno's Skia composition path. It can run on a hardware-backed canvas where supplied by the host and also supports software raster execution. This release does not implement a separate WebGPU compute backend.

Preview sources are limited to a 2560-pixel long edge. Decode, thumbnails, histogram sampling and image export still involve synchronous CPU/native raster work. Browser CI uses Chromium/SwiftShader, not physical-GPU certification. New reports distinguish CPU microbenchmarks, cache counts and real UI interactions rather than implying those measurements are end-to-end frame rates.

## Build and run

Install the SDK in `global.json`, Python 3 and the Uno desktop prerequisites for your operating system.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

# Model, pixel-processing, transaction, recovery and performance checks
dotnet run --project tests/LightSpace.Engine.Tests -c Release

# Shared native desktop host
dotnet run --project src/LightSpace.App -f net10.0-desktop \
  -p:LightSpaceDesktopOnly=true

# Publish and serve the actual Uno WebAssembly app
dotnet publish src/LightSpace.App -f net10.0-browserwasm -c Release \
  -o artifacts/publish -p:WasmShellWebAppBasePath=/LightSpace/
python3 scripts/collect-site.py artifacts/publish artifacts/site
python3 scripts/serve-site.py --directory artifacts/site --port 4173
```

Open `http://127.0.0.1:4173/LightSpace/`. The UI is compiled Uno/.NET WebAssembly, not a static HTML mockup. Serve it over HTTP(S), not by opening a local HTML file.

The asset script verifies the OFL font by hash and optionally downloads demonstration photographs at build time. Set `LIGHTSPACE_NO_DEMO_PHOTOS=1` to use original generated landscapes instead. The running application does not fetch sample photographs from third-party servers.

## Reuse the engine

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;

var photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
var session = new EditorSession(new CatalogDocument
{
    Photos = [photo], ActivePhoto = photo.Id
});
session.Edit("Alpine grade", state => state with
{
    Develop = state.Develop with
    {
        Exposure = 0.35f, Highlights = -25, Shadows = 20,
        Grading = new ColorGradingSettings
        {
            Shadows = new GradingTone(200, 12),
            Highlights = new GradingTone(40, 15), Blending = 60
        }
    }
});
using var renderer = new PhotoRenderer();
File.WriteAllBytes("mountains-edited.jpg",
    renderer.Export(photo, SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));
session.Undo(); // Original bytes were never rewritten.
```

Embed `StudioView(session, storage, recoveryLoaded)` in an Uno host, or use the public controls independently. Value editors expose Previewed/Committed/Canceled events; their host owns transactions and refreshes values after external edits. Dispose native-resource owners when their containing host closes. Snapshot arrays and source bytes are read-only/copy-on-write by contract.

```csharp
using var recovery = new RecoveryCoordinator(session, storage.WriteRecoveryAsync,
    initiallySaved: restoredFromRecovery);
recovery.StatusChanged += status => UpdateSaveIndicator(status);
await recovery.FlushAsync();
```

The session, recovery coordinator and renderer are confined to one logical owner, normally the UI synchronization context. [Ownership and embedding](docs/ARCHITECTURE.md) · [Recovery contract](docs/RECOVERY.md)

## Validation and delivery

`Build` runs the engine suite, publishes WebAssembly, drives actual Uno controls with Playwright, validates rendered exports and recovery, and packages the libraries. Reports include screenshots, failure traces, exports, source snapshots and performance evidence.

`Desktop` compiles the shared host on Windows, Linux and macOS. Compilation does not certify native file pickers, assistive technology or GPU drivers on every device.

`Pages` accepts only a successful trusted main-branch Build artifact, verifies its commit, deploys through official GitHub Pages actions, checks the public build identity and reruns browser tests against the public application.

`Release` builds packages, browser and self-contained `win-x64`, `linux-x64`, `osx-arm64` archives with checksums. Signing, notarization, installers and NuGet.org publication are not configured.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/import-fixture.png artifacts/fixtures/
npm run test:browser
```

## Compatibility and safety

Schema-1 catalogs migrate on import; new saves use **schema 2** for grading and extended masks. Older builds reject schema2 instead of silently dropping new processing settings. Keep a pre-upgrade backup where older-version interoperability matters.

Import supports JPEG, PNG, WebP, BMP and the first GIF frame. Rendered export is 8-bit sRGB, capped at an 8192-pixel long edge; source EXIF/IPTC metadata is not copied. Safety ceilings are 64 MiB per source, 100 megapixels decoded and 256 MiB of encoded originals per catalog. These limits are not recommended workloads or peak-memory guarantees.

RAW/HEIF/TIFF, AI tools, calibrated camera/lens profiles, HDR/panorama merging, tiled native-resolution inspection, cloud sync, printing/soft proofing, Adobe `.lrcat`/XMP/preset compatibility and full accessibility parity remain incomplete or absent. Recovery is one JSON record, not a concurrent database or crash journal. Retain original files and portable backups.

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Color and masks](docs/COLOR-AND-MASKS.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Coverage](docs/FEATURE-COVERAGE.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Notices](THIRD-PARTY-NOTICES.md)

LightSpace source is [MIT licensed](LICENSE). Dependencies, the OFL font and optional Unsplash photographs retain their respective licenses; photographs are not relicensed as MIT. Adobe and Lightroom are trademarks of their owners and are used only as workflow references. No endorsement or proprietary compatibility is claimed.
