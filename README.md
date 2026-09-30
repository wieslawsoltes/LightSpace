<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · Composed Skia effects · Reusable C# libraries

[Open LightSpace](https://wieslawsoltes.github.io/LightSpace/) · [Getting started](docs/GETTING-STARTED.md) · [Crop tools](docs/CROP-CONSTRAINTS.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace is a shared Uno desktop/WebAssembly photography application: browse a local catalog, develop a photograph, refine its composition and masks, and export a rendered copy while retaining the original bytes. Its dark workspace combines a library sidebar, photo canvas, filmstrip, histogram, development inspector and vertical tool rail.

**Current source version: 0.9.0-alpha.1.** This is independent, functional early-stage software—not pixel-identical or feature-complete Adobe Lightroom. No Adobe artwork, proprietary processing, calibrated camera/lens profiles or cloud services are included. The [feature ledger](docs/FEATURE-COVERAGE.md) distinguishes implemented behavior from remaining boundaries. Source version, published package version and deployed site identity are separate; `build-info.json` identifies the browser deployment.

## New in 0.9: continuous crop tools

**Lock the composition.** Preset or custom output ratios now remain constrained while dragging corners and edges. Opposite anchors remain fixed, Alt/Option resizes about the center, and Shift temporarily locks an otherwise free crop. A toggles locking, X swaps width/height, and arrow keys nudge framing. Ratios account for final quarter-turn orientation and integer output rounding.

**Compose with guides.** Thirds, Grid, Golden ratio, Diagonals, Triangle and None are available through the inspector or O. Shift+O reverses triangle orientation. Guides are direct, bounded Skia line geometry—not processing data or exported overlays.

**Keep gestures safe.** Crop input captures a source, pointer and coordinate frame. Root size, scale, visibility and arranged-position changes invalidate capture and restore its opening state. Listeners exist only while captured; release cannot commit a canceled gesture. Tests hold capture until Uno acknowledges resize cancellation rather than racing pointer-up against browser viewport emulation.

**Avoid redundant work.** The analytic crop/guide loop uses caller-owned storage; its warmed allocation test covers 100,000 evaluations. Crop-only adjustments reuse source, development, optical and curve caches. The full-frame crop is shared immutable state. These are scoped CPU/work-avoidance measurements, not a claim that the whole application is allocation-free or GPU-only. [Behavior, reusable API and evidence](docs/CROP-CONSTRAINTS.md)

Catalog schema **6**, native XMP settings **5**, recovery manifest **1** and IndexedDB **2** are unchanged. Final crop bounds persist; locks and guides are session-local tool state. **Reload without clearing site data.** Keep pre-upgrade portable backups when using older builds.

## Photography workflows

**Organize and cull.** Import JPEG, PNG, WebP, BMP or GIF. Browse grid/detail/filmstrip views, search names/captions/keywords, rate and flag photographs, and organize albums. Survey or N compares twelve candidates per page with reversible exclusions and single-target metadata editing; exclusion never deletes catalog records. Virtual copies persist independent looks while sharing original bytes. [Survey](docs/SURVEY.md) · [Virtual copies](docs/VIRTUAL-COPIES.md)

**Develop.** Adjust tone, relative white balance, saturation/vibrance, monochrome and eight hue bands. Master/R/G/B curves support 32 points, numeric/pointer/keyboard editing and linear or shape-preserving interpolation. Four-way grading controls shadows, midtones, highlights and global tint. Creative presets, detail/effect approximations, sharpening, spatial smoothing, grain and vignette provide further tools.

**Refine.** Use constrained/free cropping, custom ratios, quarter turns/flips, drawn-horizon straightening and manual projective geometry. Correct manual distortion, lens falloff and radial channel alignment. Create gradients, luminance ranges, five-color Oklab selections and paint/erase masks. Source-patch white balance, interactive histogram and display-only clipping indicators support evaluation. [Crop](docs/CROP-CONSTRAINTS.md) · [Optics/geometry](docs/OPTICS-GEOMETRY.md) · [Color and masks](docs/COLOR-AND-MASKS.md)

**Compare and transfer.** Shift+R pins a frozen reference beside active editing. Link or separate fit-relative navigation, switch side-by-side/stacked views and selectively match the pinned look. Copy/paste and synchronization offer thirteen processing groups in one reviewed transaction while retaining target metadata. References are session-local; virtual copies persist. [Reference/settings](docs/REFERENCE-AND-SYNC.md)

**Preserve alternatives.** Virtual copies have independent edits, metadata and versions; creation, rename and copy-only removal are undoable. Families retain source and album relationships without duplicating original payloads. Removing an active copy keeps the displayed photograph within the surviving selection. [Family API](docs/VIRTUAL-COPIES.md) · [Selection safeguards](docs/VIRTUAL-COPY-SELECTION.md)

**Stay local.** Completed gestures and structural copy operations participate in undo. Export JPEG/PNG/WebP, ZIP selections, save portable catalogs or exchange XMP. Native XMP round-trips a look's processing, not its family identity; Camera Raw fields are an explicit subset, not Adobe pixel equivalence. Recovery stores SHA-256 originals separately from committed manifests, checks integrity and protects unreadable data. [XMP](docs/ADVANCED-EDITING.md) · [Recovery](docs/RECOVERY.md)

## Download and distribution

The [release workflow](https://github.com/wieslawsoltes/LightSpace/releases) produces self-contained, single-file applications for Windows, macOS and Linux, each with x64 and Arm64 variants. No separate .NET installation is needed. Select an actually published version and verify `SHA256SUMS.txt`.

Windows archives use `LightSpace-<version>-win-<architecture>.zip`; Linux/macOS use `.tar.gz` with their corresponding RID. Extract and run LightSpace (LightSpace.exe on Windows). Native dependencies extract at startup. Builds are not signed or notarized. A source version change does not publish release assets. [Release contract](docs/RELEASES.md)

## Eight reusable NuGet libraries

All eight libraries are MIT-licensed and versioned together with `.snupkg` symbols and SourceLink. Six engine packages target `net10.0` without Uno; Imaging and Rendering.Skia use SkiaSharp. Controls and Workbench target Uno desktop/browser. The public feed and current source version may differ; build artifacts do not establish NuGet.org publication. New APIs require packages built from their corresponding source revision.

| Package | Public version | Responsibility |
| --- | --- | --- |
| [LightSpace.Core](https://www.nuget.org/packages/LightSpace.Core) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | Photo/copy state, crop interaction and guides, curves, grading, masks, geometry and settings groups |
| [LightSpace.Storage](https://www.nuget.org/packages/LightSpace.Storage) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | Import/export, atomic recovery and optional sidecar picking |
| [LightSpace.Catalog](https://www.nuget.org/packages/LightSpace.Catalog) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | Migration, family/source validation, queries, manifests and XMP |
| [LightSpace.Imaging](https://www.nuget.org/packages/LightSpace.Imaging) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | Bounded decoding, EXIF orientation, sRGB conversion and generated samples |
| [LightSpace.Rendering.Skia](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | Composed effects, geometry, shared-source caches, masks, histogram and export |
| [LightSpace.Editing](https://www.nuget.org/packages/LightSpace.Editing) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | Transactions, copies, history, references, selective sync and recovery |
| [LightSpace.Controls](https://www.nuget.org/packages/LightSpace.Controls) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | Reusable crop/editing controls, histogram, thumbnails and active/reference/Survey views |
| [LightSpace.Workbench](https://www.nuget.org/packages/LightSpace.Workbench) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | Composable workspace, copy manager, catalog, tools, dialogs and recovery UX |

Install the layer needed by the host:

```sh
dotnet add package LightSpace.Core --prerelease
dotnet add package LightSpace.Rendering.Skia --prerelease
dotnet add package LightSpace.Workbench --prerelease
```

Dependencies: `Core ← Catalog, Imaging`; `Imaging ← Rendering.Skia`; `Catalog + Storage ← Editing`; `Editing + Rendering.Skia ← Controls`; `Controls + Storage ← Workbench`. Storage has no project dependencies. Standard Uno input, scrolling and accessibility primitives remain where appropriate.

### Core: geometry and immutable state

Key types include PhotoState, DevelopSettings, CropSettings, CropGeometry, CropGesture, CropGuides, GeometrySettings, LensCorrectionSettings, PointCurve/ChannelCurves, ColorGradingSettings, LocalMask, BrushStroke, PhotoDocument, VirtualCopyNames, EditSettingsTransfer and SurveyLayout.

```csharp
using LightSpace.Core;

var crop = CropGeometry.WithAspect(new CropSettings(), 16d / 9, 6000, 4000);
var gesture = new CropGesture(crop, CropHandle.BottomRight,
    new(crop.Right, crop.Bottom), 6000, 4000, aspectLocked: true);
crop = CropGeometry.Apply(crop, gesture.Evaluate(new(.8, .7)));
Span<CropGuideLine> guides = stackalloc CropGuideLine[CropGuides.MaximumLines];
int count = CropGuides.Write(CropGuide.Thirds, new(0, 0, 900, 600), false, guides);
```

[Crop contracts](docs/CROP-CONSTRAINTS.md) · [Curve/brush models](docs/ADVANCED-EDITING.md) · [Optical/projective math](docs/OPTICS-GEOMETRY.md)

### Storage and Catalog: persistence and interchange

IWorkspaceStorage supplies picker/export/recovery operations. IRecoveryStore and FileRecoveryStore stage content-addressed originals and publish a validated manifest. RecoveryWrite, RecoveryBlob and RecoveryKeys describe that contract; ISidecarStorage is optional.

CatalogSerializer validates and migrates catalogs; VirtualCopyCatalog validates families. PhotoQuery supports metadata filters, PhotoSort and PhotoKindFilter. XmpSidecar reports supported and unsupported fields before application.

```csharp
using LightSpace.Catalog;
using LightSpace.Core;

CatalogDocument catalog = CatalogSerializer.Deserialize(File.ReadAllText("catalog.lightspace"));
var copies = new PhotoQuery { Kind = PhotoKindFilter.VirtualCopies }.Execute(catalog);
var picks = new PhotoQuery(Text: "alps", MinimumRating: 3,
    Flag: PhotoFlag.Pick, Sort: PhotoSort.Rating).Execute(catalog);
File.WriteAllText("backup.lightspace", CatalogSerializer.Serialize(catalog));
```

[Recovery API](docs/RECOVERY.md) · [Copy representation](docs/VIRTUAL-COPIES.md) · [XMP mappings](docs/ADVANCED-EDITING.md#xmp-interchange)

### Imaging, Rendering and Editing: non-destructive processing

PhotoCodec imports retained originals and decodes bounded orientation-normalized previews. PhotoRenderer supplies Prepare, Draw, Export, CalculateHistogram, Auto, SampleSource and ReleasePhoto. It owns source, curve and brush caches with explicit lifetimes; execution is GPU-backed or software according to its canvas.

EditorSession owns Preview/CommitGesture/CancelGesture, history, versions, selected settings and reversible copy operations. All targets are staged before a batch changes the catalog. Source arrays and nested snapshot arrays are immutable by contract.

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using LightSpace.Storage;
using SkiaSharp;

var original = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
var session = new EditorSession(new CatalogDocument { Photos = [original], ActivePhoto = original.Id });
var copy = session.CreateVirtualCopies([original.Id]).Single();
session.RenameVirtualCopy(copy.Id, "Warm alternative");
session.Edit("Warm and frame", state => state with
{
    Develop = state.Develop with { Exposure = .35f, Temperature = 15 },
    Crop = CropGeometry.WithAspect(state.Crop, 16d / 9, copy.Width, copy.Height)
});
using var renderer = new PhotoRenderer();
File.WriteAllBytes("warm-alternative.jpg", renderer.Export(copy,
    SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));

var persistence = new RecoveryPersistence(new FileRecoveryStore("recovery"));
using var recovery = RecoveryCoordinator.Incremental(session, persistence, initiallySaved: false);
await recovery.FlushAsync();
session.Undo(); // Original bytes remain untouched.
```

[Editing and copies](docs/VIRTUAL-COPIES.md#reusable-api-and-ownership) · [Selective groups](docs/REFERENCE-AND-SYNC.md) · [Renderer ownership](docs/ARCHITECTURE.md)

### Controls and Workbench: embedding in Uno

Reusable controls include PhotoViewport, ReferencePhotoView, PhotoSurveyView, CropAspectEditor, SettingsTransferEditor, AdjustmentSlider, PointCurveEditor, ColorGradingEditor, ColorMixerEditor, ColorRangeEditor, MaskSettingsEditor, GeometryEditor, OpticsEditor, BrushSettingsEditor, HistogramView, PanelResizeGrip and PhotoCard. Value editors emit preview/commit/cancel events; the host supplies transactions and refreshes values after external edits.

```csharp
using LightSpace.Controls;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;

var session = new EditorSession(catalog);
var renderer = new PhotoRenderer();
var viewport = new PhotoViewport(session, renderer);
var exposure = new AdjustmentSlider("Exposure", -5, 5, 0, .01);
exposure.ValueChanged += value => session.Preview(s => s with
{
    Develop = s.Develop with { Exposure = value }
});
exposure.ValueCommitted += () => session.CommitGesture("Exposure");
exposure.GestureCanceled += session.CancelGesture;
session.ViewChanged += () => exposure.Value = session.Active?.State.Develop.Exposure ?? 0;
// Insert the controls into the host layout. On teardown:
// viewport.Dispose(); renderer.Dispose();
```

StudioView supplies the full workspace and accepts a session, IWorkspaceStorage and optional restored RecoveryPersistence. UnsavedChangesChanged supports unload protection; DiagnosticsChanged is opt-in. The supplied application protects failed recovery rather than silently replacing it.

```csharp
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Storage;
using LightSpace.Workbench;

IWorkspaceStorage storage = new MyWorkspaceStorage(); // Host adapter.
var persistence = storage is IRecoveryStore store ? new RecoveryPersistence(store) : null;
var restored = persistence is null ? null : await persistence.RestoreAsync();
var studio = new StudioView(new EditorSession(restored ?? SamplePhotos.CreateCatalog()),
    storage, recoveryLoaded: restored is not null, persistence: persistence);
window.Content = studio;
window.Closed += (_, _) => studio.Dispose();
```

A production host must catch restore failures and preserve prior data; follow the supplied App startup and ProtectExistingRecovery implementation. Dispose native caches, unsubscribe externally held listeners, and serialize access on one logical owner. [Embedding contracts](docs/ARCHITECTURE.md) · [Crop control](docs/CROP-CONSTRAINTS.md) · [Reference wiring](docs/REFERENCE-AND-SYNC.md#reusable-model-and-ui-apis)

## Stack and rendering

Pinned: **.NET SDK 10.0.401**, **Uno SDK 6.7.30**, matched **SkiaSharp 3.119.4** managed/native assets. Upgrade the graphics family together and retain ABI/lifetime tests.

SKCanvasElement integrates composed effects in Uno's Skia path. Hardware execution requires a GPU-backed canvas; software remains supported. This is **not a separate WebGPU engine** or GPU-only import/export pipeline. CPU/native work remains in decoding, first-use hashing, manifest serialization, brush preparation, histograms and encoding.

Preview targets are 2560 pixels for editing, 384 for thumbnails, 1024 for Survey and brush coverage, with output capped at 8192. Budgets bound retained buffers, not all transient/GPU memory. Channel alignment can evaluate development three times. Copies/references share sources within their renderer; distinct visible looks still shade separately. [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md)

## Build and run

Install the pinned SDK, Python 3 and the host OS's Uno desktop prerequisites.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

dotnet run --project tests/LightSpace.Engine.Tests -c Release

dotnet run --project src/LightSpace.App -f net10.0-desktop \
  -p:LightSpaceDesktopOnly=true

dotnet publish src/LightSpace.App -f net10.0-browserwasm -c Release \
  -o artifacts/publish -p:WasmShellWebAppBasePath=/LightSpace/
python3 scripts/collect-site.py artifacts/publish artifacts/site
python3 scripts/serve-site.py --directory artifacts/site --port 4173
```

Open `http://127.0.0.1:4173/LightSpace/`. Serve Uno/.NET WebAssembly over HTTP(S), not as a local HTML file. The asset script verifies the OFL font and optionally downloads demonstration photographs at build time; `LIGHTSPACE_NO_DEMO_PHOTOS=1` uses original generated landscapes.

## Tests and delivery gates

Build runs model/processing/ownership tests, publishes WebAssembly, drives actual controls, checks exports/recovery, then packs and audits libraries. Desktop compiles Windows/Linux/macOS. Pages verifies the trusted main artifact's commit and repeats acceptance on the public site. Reports distinguish branch, merged-main and public-site results.

The fatal-slider regression drives all seventeen main adjustments through extremes and neutral crossings, then verifies undo/redo, JPEG decoding, every final catalog value and recovery reload. Crop, photography, masks, reference, Survey and copy tests verify real input, pixels, safe structural operations and source integrity. Features and fatal-error reporting remain enabled.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/*.png artifacts/fixtures/
npm run test:browser
npx playwright test crop-constraints.spec.mjs crop-lifetime.spec.mjs
npx playwright test slider-stability.spec.mjs
python3 -m unittest discover -s tests/scripts -v
python3 scripts/validate-packages.py --commit "$(git rev-parse HEAD)"
```

Package audits reject missing/mixed package-symbol pairs, wrong versions/commits, inconsistent internal dependencies, missing target payloads, unsafe paths and incomplete metadata. ZIP integrity and SHA-256 are recorded without executing assemblies. They do not prove installation or publication.

Release retains six-RID single-file packaging, browser archives, symbols and checksums. Tags create releases and publish with OIDC Trusted Publishing through the protected nuget environment. Manual runs are dry runs that build/upload artifacts. A source commit implies neither a tag nor publication. [Release documentation](docs/RELEASES.md)

CI uses Chromium/SwiftShader, not physical GPUs or certified pen hardware. CPU timings, memory capacity and work counters are scoped evidence, not universal speed or leak-free claims. Signing/notarization, installers and automatic updates remain unconfigured.

## Compatibility and safety

Catalog **6** preserves copy families and migrates versions 1–5. Native XMP **5** accepts 3–4 and records processing, not family identity. Recovery manifest **1** and IndexedDB **2** are unchanged. Crop constraints add no serialized fields. Older builds reject unsupported schemas. Keep portable pre-upgrade backups; reload without deleting site data.

Limits: 64 MiB per source, 100 megapixels decoded, 256 MiB catalog source bytes and 5,000 records including copies. Output is 8-bit sRGB, at most 8192 pixels, without embedded source EXIF/IPTC. Recovery is local/unencrypted with no cross-tab merge or orphan cleanup. Retain original files and portable backups.

RAW/DNG/HEIF/TIFF, AI tools, calibrated profiles, automatic/guided Upright, HDR/panorama, native-resolution tiling, indexed durable catalogs, master promotion/stacks, printing/proofing and cloud workflows remain unimplemented. Crop guides do not include a golden spiral. The UI is Lightroom-inspired, not full pixel parity. [Full feature ledger](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Crop constraints](docs/CROP-CONSTRAINTS.md) · [Virtual copies](docs/VIRTUAL-COPIES.md) · [Survey](docs/SURVEY.md) · [Reference/settings](docs/REFERENCE-AND-SYNC.md) · [Optics/geometry](docs/OPTICS-GEOMETRY.md) · [Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Slider fix](docs/WASM-SLIDER-FIX.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

Source is [MIT licensed](LICENSE). Dependencies, OFL font and optional Unsplash photographs retain their licenses; photographs are not MIT-relicensed. Adobe and Lightroom are comparative workflow references and trademarks of their owners. No affiliation, endorsement or proprietary catalog compatibility is claimed.
