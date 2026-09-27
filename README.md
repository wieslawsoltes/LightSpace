<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · GPU-capable Skia runtime effects · Eight reusable C# libraries

[Open the browser application](https://wieslawsoltes.github.io/LightSpace/) · [Getting started](docs/GETTING-STARTED.md) · [Architecture](docs/ARCHITECTURE.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace combines a familiar photography layout with a shared Uno desktop/WebAssembly implementation: a library sidebar, centered photo canvas, filmstrip, histogram, development inspector, and vertical tool rail. Photographs remain local, original bytes are retained, and edits are applied non-destructively.

**Current version: 0.1.1-alpha.1.** This is a functional early implementation, not a pixel-identical or feature-complete Adobe Lightroom replacement. It is independent software and includes no Adobe branding, artwork, camera profiles, proprietary processing code or cloud services. The [feature ledger](docs/FEATURE-COVERAGE.md) separates implemented behavior from remaining gaps.

## What changed in 0.1.1

Recovery now tracks **committed revisions**, not live slider previews or notification text. An earlier save cannot acknowledge a newer edit. In-flight writes are serialized; subsequent commits are drained afterward; failed writes remain unsaved and can be retried. The footer has a dedicated save indicator and explicit save/retry control, and browser navigation warns about unsaved work.

Unreadable recovery is protected until the user explicitly confirms replacement. Reopening catalogs that reuse photo IDs or revision zero invalidates source/shader/thumbnail caches correctly. Escape cancels slider gestures without committing a partial value.

[Changelog](CHANGELOG.md) · [Recovery contract and regression tests](docs/RECOVERY.md)

## Photography workflow

**Organize.** Import multiple JPEG, PNG, WebP, BMP or GIF photographs. Browse a paged grid or filmstrip, search filenames/captions/keywords, assign ratings and flags, and collect photo references in albums. Control-click extends a selection; copied settings and synchronization work across selected photographs.

**Develop.** Adjust exposure, contrast, highlights, shadows, whites, blacks, relative white balance, vibrance and saturation. Edit five-point curves and eight color bands, use creative presets, and adjust texture, clarity, dehaze, vignette, grain, sharpening and spatial smoothing. These are original algorithms and approximations, not Adobe processing or AI denoising.

**Refine.** Drag crop rectangles and handles, apply centered aspect ratios, rotate in quarter turns and flip. Create radial or vertical linear-gradient masks with local exposure/saturation and feathering. Alt-click a clone source and stamp destinations. Pan, zoom, fit and compare the edited photograph with its original.

**Preserve.** Undo/redo coalesces each gesture into a transaction. Named versions preserve alternative looks. Export rendered JPEG/PNG/WebP copies, ZIP a multi-photo selection, or export a portable `.lightspace` catalog containing original bytes, metadata, edits and albums. Browser recovery uses IndexedDB; native recovery uses the platform application-data directory.

## Reusable libraries

| Library | Responsibility | Targets |
| --- | --- | --- |
| `LightSpace.Core` | Development, crop/mask geometry, photo/catalog models and presets | .NET 10 |
| `LightSpace.Catalog` | Versioned serialization, validation, search, filtering and album queries | .NET 10 |
| `LightSpace.Editing` | Gesture transactions, undo/redo, batch synchronization, versions, revision-aware recovery | .NET 10 |
| `LightSpace.Imaging` | Bounded image decoding, EXIF orientation, sRGB conversion and fallback samples | .NET 10 |
| `LightSpace.Rendering.Skia` | Runtime-effect development, image caching, transforms, histogram and export | .NET 10 |
| `LightSpace.Storage` | Platform-independent import, export and recovery contracts | .NET 10 |
| `LightSpace.Controls` | Custom buttons/icons, sliders, curves, histogram, thumbnails and interactive viewport | Uno browser / desktop |
| `LightSpace.Workbench` | Composable photography workspace, inspectors, commands, dialogs and local save UX | Uno browser / desktop |

Engine libraries do not depend on Uno. The host supplies startup and platform storage. Buttons, iconography, slider rails/knobs, curves, histograms, thumbnails and photo interactions are custom components. Standard Uno text-input, scrolling, selection and accessibility primitives remain in use where appropriate; not every primitive has been reimplemented.

Successful Build runs produce eight `.nupkg` artifacts. This does not imply publication to NuGet.org.

## Rendering and pinned stack

The repository pins **.NET SDK 10.0.401**, **Uno SDK 6.7.30** and the matched **SkiaSharp 3.119.4** family. Managed/native graphics packages must be upgraded together and validated against Uno's host ABI.

The viewport renders through `Uno.WinUI.Graphics2DSK.SKCanvasElement`. Development uses a compiled SkSL runtime effect inside Uno's Skia composition path, allowing hardware execution when the host provides a GPU-backed canvas and portable software execution otherwise. This release does **not** have a separate WebGPU compute backend.

Decode, thumbnail generation, histogram sampling and image export still use CPU/offscreen raster work. Preview decoding is capped at a 2560-pixel long edge. CI uses Chromium/SwiftShader and does not certify physical-GPU performance. [Pipeline, ownership and extension points →](docs/ARCHITECTURE.md)

## Build and run

Install the SDK specified in `global.json`, Python 3 and your platform's Uno desktop prerequisites.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

# Model, rendering, transaction and asynchronous recovery tests
dotnet run --project tests/LightSpace.Engine.Tests -c Release

# Native desktop host
dotnet run --project src/LightSpace.App -f net10.0-desktop \
  -p:LightSpaceDesktopOnly=true

# Real Uno WebAssembly application
dotnet publish src/LightSpace.App -f net10.0-browserwasm -c Release \
  -o artifacts/publish -p:WasmShellWebAppBasePath=/LightSpace/
python3 scripts/collect-site.py artifacts/publish artifacts/site
python3 scripts/serve-site.py --directory artifacts/site --port 4173
```

Open `http://127.0.0.1:4173/LightSpace/`. Serve the browser app over HTTP(S), not by opening a local HTML file. Its UI is compiled Uno/.NET WebAssembly, not an HTML mockup.

The asset script hash-verifies the OFL font and optionally downloads sample photographs at build time. Set `LIGHTSPACE_NO_DEMO_PHOTOS=1` to use original generated landscapes instead. The running app does not fetch sample photographs from third-party servers.

## Embed the engine or workspace

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
session.Edit("Lift the light", state => state with
{
    Develop = state.Develop with { Exposure = 0.35f, Highlights = -25, Shadows = 20 }
});
using var renderer = new PhotoRenderer();
File.WriteAllBytes("mountains-edited.jpg",
    renderer.Export(photo, SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));
session.Undo(); // Original bytes were never rewritten.
```

Embed `StudioView(session, storage, recoveryLoaded)` in an Uno host, or use the individual public controls. Dispose the workbench when its containing host closes. `PhotoViewport` does not own an externally supplied renderer. Snapshot arrays and source bytes are copy-on-write/read-only by contract; do not mutate them in place.

For another host's recovery UI:

```csharp
using var recovery = new RecoveryCoordinator(session, storage.WriteRecoveryAsync,
    initiallySaved: restoredFromRecovery);
recovery.StatusChanged += status => UpdateSaveIndicator(status);
await recovery.FlushAsync();
```

The coordinator and session are confined to one logical owner, normally the UI synchronization context. The storage delegate must complete only after its write commits. See [recovery invariants](docs/RECOVERY.md) and [embedding/ownership](docs/ARCHITECTURE.md#embedding-and-ownership).

## Validation and delivery

`Build` runs the engine suite, publishes WebAssembly, drives actual Uno controls with Playwright, checks image export and recovery, and packages the reusable libraries. It uploads reports, screenshots, failure traces, exports and source snapshots.

`Desktop` compiles the shared host on Windows, Linux and macOS. Compilation is not device-level certification of native file pickers, accessibility or GPU drivers.

`Pages` deploys only artifacts from a successful trusted main-branch Build run. It verifies commit provenance, publishes through GitHub Pages, checks the public build identity and repeats acceptance tests against the deployed application.

`Release` builds packages, a browser archive and self-contained `win-x64`, `linux-x64` and `osx-arm64` archives, with checksums. Signing, notarization, installers and NuGet.org publication are not configured.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/import-fixture.png artifacts/fixtures/
npm run test:browser
```

## Formats, limits and remaining gaps

Sources are retained byte-for-byte. Import supports JPEG, PNG, WebP, BMP and the first decoded GIF frame. Camera RAW, HEIF/HEIC, TIFF, Lightroom `.lrcat`, Adobe profiles/presets and XMP interchange are not implemented.

Rendered exports are 8-bit sRGB, capped at an 8192-pixel long edge; source EXIF/IPTC metadata is not copied. Safety ceilings are 64 MiB per source, 100 megapixels decoded and 256 MiB of encoded originals per catalog. Browsing uses 60-photo pages rather than a production-scale indexed database. Recovery is one JSON record, not a crash journal or concurrent-tab merge system. Keep original files and portable backups.

Other remaining areas include AI selection/removal/denoising, calibrated camera/lens correction, HDR/panorama merging, floating-point RAW processing, native-resolution tiled inspection, printing/soft proofing, cloud synchronization, and complete keyboard/accessibility parity. [Full feature ledger →](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide and shortcuts](docs/GETTING-STARTED.md) · [Architecture](docs/ARCHITECTURE.md) · [Recovery](docs/RECOVERY.md) · [Coverage](docs/FEATURE-COVERAGE.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

LightSpace source is [MIT licensed](LICENSE). Dependencies, the OFL font and optional Unsplash photographs retain their own licenses; sample photographs are not relicensed as MIT. Adobe and Lightroom are trademarks of their respective owners and are used only as workflow references. No endorsement or proprietary catalog compatibility is claimed.
