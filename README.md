<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · Composed Skia effects · Reusable C# libraries

[Open LightSpace](https://wieslawsoltes.github.io/LightSpace/) · [Guide](docs/GETTING-STARTED.md) · [Virtual copies](docs/VIRTUAL-COPIES.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace is a shared Uno desktop/WebAssembly photography application: browse a local catalog, develop a photograph, refine its composition and masks, and export a rendered copy while retaining the original bytes. The dark workspace combines a library sidebar, centered photo canvas, filmstrip, histogram, development inspector and vertical tool rail.

**Current source version: 0.8.0-alpha.1.** This is independent, functional early-stage software—not pixel-identical or feature-complete Adobe Lightroom. No Adobe artwork, proprietary processing, calibrated camera/lens profiles or cloud services are included. [Implemented behavior and remaining boundaries →](docs/FEATURE-COVERAGE.md)

## New in 0.8

**Persistent virtual copies.** Explore different looks without duplicating original image files. Create one copy or copies for a selection, rename them, filter originals/copies, and compare an original's family in Survey. Each copy has independent processing, metadata and named versions. Creating, renaming and removing copies are undoable; copy-only removal cannot delete original records or source files.

**Shared sources, independent pixels.** Copies share immutable encoded bytes and matching decoded previews, while retaining independent processing identities. Portable schema-6 catalogs write the original payload once per family and hydrate a shared array on load. Recovery reuses the existing SHA-256 blob. Warm renames do not decode images, rebuild shaders, rehash originals or rewrite stored source blobs.

**Safe selection and delivery.** Removing an active copy from a multi-selection now activates a selected survivor rather than displaying an unselected original. Undo/redo restore the corresponding selection. Tests check distinct pixels, original preservation, copy-only removal, portable family reconstruction, filters, Survey handoffs and reload. The fatal-slider regression remains enabled. CI audits all eight package/symbol pairs for version, commit, dependencies, payloads and metadata. [Virtual-copy guide](docs/VIRTUAL-COPIES.md) · [Selection safeguard](docs/VIRTUAL-COPY-SELECTION.md)

Catalog schema **6** migrates versions 1–5. Native XMP settings remain **5**, recovery manifest **1** and IndexedDB **2**. Older builds reject schema 6. Keep pre-upgrade portable backups for interoperability. **Do not clear site data** to load this update; it contains recovery. `build-info.json` identifies the deployed version and commit.

## Photography workflows

**Organize and cull.** Import JPEG, PNG, WebP, BMP or GIF. Browse grid/detail/filmstrip views, search names/captions/keywords, rate/flag photos and organize albums. Survey or N compares twelve candidates per page with reversible exclusions and single-target metadata editing; exclusion never deletes catalog records. Virtual copies persist alternative looks. [Survey](docs/SURVEY.md) · [Virtual copies](docs/VIRTUAL-COPIES.md)

**Develop.** Adjust tone, relative white balance, saturation/vibrance, monochrome and eight hue bands. Master/R/G/B curves support 32 points, numeric/pointer/keyboard editing and linear or shape-preserving interpolation. Four-way grading controls shadows, midtones, highlights and global tint. Creative presets, detail/effect approximations, sharpening, spatial smoothing, grain and vignette provide further tools.

**Refine.** Use crop handles, centered ratio presets, quarter turns/flips, drawn-horizon straightening and manual projective geometry. Correct manual distortion, lens falloff and radial channel alignment. Create gradients, luminance ranges, five-color Oklab selections and paint/erase masks. A source-patch white-balance picker, interactive histogram and display-only clipping indicators support evaluation. [Optics/geometry](docs/OPTICS-GEOMETRY.md) · [Color and masks](docs/COLOR-AND-MASKS.md)

**Compare and transfer.** Shift+R pins a frozen reference beside active editing. Link or separate fit-relative navigation, switch side-by-side/stacked views, and selectively match the pinned look. Copy/paste and synchronization offer thirteen processing groups in one reviewed transaction while retaining target metadata. References are session-local; virtual copies persist. [Reference/settings](docs/REFERENCE-AND-SYNC.md)

**Preserve.** Gestures are undo transactions and named versions retain states. Export JPEG/PNG/WebP, ZIP selections, save compact family catalogs or exchange XMP. Native LightSpace XMP round-trips a look's processing, not its family identity. The reported Camera Raw subset is not equivalent Adobe development. [Advanced editing/XMP](docs/ADVANCED-EDITING.md)

**Stay local.** Revision-aware recovery stores SHA-256 originals and committed edit manifests. Warm metadata saves avoid rewriting or rehashing unchanged originals. Restore checks integrity and family relationships; failures stay dirty and unreadable data is protected. No account, photo upload or cloud processing is required. [Recovery](docs/RECOVERY.md)

## Download

The [release workflow](https://github.com/wieslawsoltes/LightSpace/releases) produces self-contained, single-file desktop applications; no separate .NET installation is needed. Select an actual published version and the matching architecture:

| OS | x64 | Arm64 |
| --- | --- | --- |
| Windows | `LightSpace-<version>-win-x64.zip` | `LightSpace-<version>-win-arm64.zip` |
| macOS | `LightSpace-<version>-osx-x64.tar.gz` | `LightSpace-<version>-osx-arm64.tar.gz` |
| Linux | `LightSpace-<version>-linux-x64.tar.gz` | `LightSpace-<version>-linux-arm64.tar.gz` |

Extract and run `LightSpace` (`LightSpace.exe` on Windows). Native dependencies extract at startup. Builds are not signed/notarized; verify source and `SHA256SUMS.txt` before opening. A source version bump does not itself publish release assets. [Packaging/publication](docs/RELEASES.md)

## NuGet packages

All eight libraries are MIT-licensed. Badges show public-feed versions/downloads independently of the source version. Six engine packages target `net10.0` without Uno; Imaging and Rendering.Skia use SkiaSharp. Controls and Workbench target desktop/browser with Uno's Skia renderer. Packages are versioned together with `.snupkg` symbols and SourceLink. Build artifacts do not imply NuGet.org publication. New APIs require packages built from the corresponding source version.

```sh
dotnet add package LightSpace.Core --prerelease
```

| Package | Version | Downloads | Description |
| --- | --- | --- | --- |
| [LightSpace.Core](https://www.nuget.org/packages/LightSpace.Core) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | Photo/copy state, curves, grading, masks, geometry and processing groups |
| [LightSpace.Storage](https://www.nuget.org/packages/LightSpace.Storage) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | Import/export, atomic recovery and optional sidecar picking |
| [LightSpace.Catalog](https://www.nuget.org/packages/LightSpace.Catalog) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | Migration, family/source validation, queries, manifests and XMP |
| [LightSpace.Imaging](https://www.nuget.org/packages/LightSpace.Imaging) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | Bounded decoding, EXIF orientation, sRGB and procedural samples |
| [LightSpace.Rendering.Skia](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | Effects, geometry, source caches, masks, histogram and export |
| [LightSpace.Editing](https://www.nuget.org/packages/LightSpace.Editing) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | Transactions, virtual copies, history, references, sync and recovery |
| [LightSpace.Controls](https://www.nuget.org/packages/LightSpace.Controls) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | Uno editors, histogram, thumbnails, active/reference/Survey and settings views |
| [LightSpace.Workbench](https://www.nuget.org/packages/LightSpace.Workbench) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | Workspace, copy manager, catalog, tools, dialogs and recovery UX |

Dependencies: `Core ← Catalog, Imaging`; `Imaging ← Rendering.Skia`; `Catalog + Storage ← Editing`; `Editing + Rendering.Skia ← Controls`; `Controls + Storage ← Workbench`. Storage has no project dependencies. The host supplies startup and platform storage. Standard Uno input, scrolling and accessibility primitives remain where appropriate.

Dispose workspaces and native caches. Treat snapshots as copy-on-write and originals as read-only. Objects have one logical owner, normally the UI synchronization context. [Component API](docs/ADVANCED-EDITING.md) · [Reference API](docs/REFERENCE-AND-SYNC.md) · [Recovery invariants](docs/RECOVERY.md)

### LightSpace.Core

UI-independent state includes development, crop, optics, geometry, masks, clones and metadata. Curves support linear or shape-preserving interpolation. Grading, ranges, brushes, transfers, Survey layout and copy identity are independent of UI/rendering. Settings normalize supported values.

```sh
dotnet add package LightSpace.Core --prerelease
```

**Key types:** `PhotoState`, `DevelopSettings`, `CropSettings`, `CloneSpot`, `GeometrySettings`, `LensCorrectionSettings`, `GeometryProjection`, `ProjectiveTransform`, `LensMapping`, `WhiteBalanceEstimator`, `PointCurve`, `ChannelCurves`, `CompiledPointCurve`, `ColorGradingSettings`, `GradingTone`, `ColorBand`, `LocalMask`, `ColorRangeSettings`, `BrushStroke`, `BrushStrokeBuilder`, `CatalogDocument`, `PhotoDocument`, `VirtualCopyNames`, `Album`, `NamedVersion`, `BuiltInPresets`, `EditSettingsGroup`, `EditSettingsTransfer`, `PhotoNavigationState`, `SurveyLayout`.

```csharp
using LightSpace.Core;

var state = new PhotoState
{
    Develop = BuiltInPresets.All.First(p => p.Name == "Golden hour").Settings with { Exposure = .3f },
    Crop = new CropSettings(Left: .05f, Top: .05f, Right: .95f, Bottom: .9f),
    Geometry = new GeometrySettings { Rotate = 1.5f, ConstrainCrop = true },
    Optics = new LensCorrectionSettings(Distortion: 12),
    Masks = [new LocalMask { Name = "Sky", Kind = MaskKind.Linear, Y = .2f, Exposure = -.4f }],
    Rating = 4, Flag = PhotoFlag.Pick, Keywords = ["alps", "sunset"]
}.Normalize();
var develop = state.Develop.Set(nameof(DevelopSettings.Contrast), 20);
var curve = new PointCurve { Points = [new(0, 0), new(.25f, .18f), new(.75f, .82f), new(1, 1)] };
float midtone = curve.Compile().Evaluate(.5f);
(int width, int height) = state.Crop.OutputSize(6000, 4000);
var transfer = new EditSettingsTransfer(state, EditSettingsGroup.Light | EditSettingsGroup.WhiteBalance);
var target = transfer.Apply(new PhotoState { Caption = "Retained target metadata" });
```

### LightSpace.Storage

Picker, download and recovery contracts. `FileRecoveryStore` stages SHA-256 originals and replaces a manifest after required sources exist. No UI or project dependencies.

```sh
dotnet add package LightSpace.Storage --prerelease
```

**Key types:** `IWorkspaceStorage` (OpenImagesAsync/OpenCatalogAsync/SaveAsync/ReadRecoveryAsync/WriteRecoveryAsync), `WorkspaceFile`, `IRecoveryStore` (ReadManifestAsync/ReadBlobAsync/CommitAsync), `FileRecoveryStore`, `RecoveryWrite`, `RecoveryBlob`, `RecoveryKeys`, optional `ISidecarStorage`.

```csharp
using LightSpace.Storage;

IRecoveryStore store = new FileRecoveryStore(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "recovery"));
// manifestJson must be a valid recovery manifest describing these sources.
byte[] original = File.ReadAllBytes("mountains.jpg");
string key = RecoveryKeys.Hash(original);
await store.CommitAsync(new RecoveryWrite(manifestJson, [key], [new RecoveryBlob(key, original)]));
string? manifest = await store.ReadManifestAsync();
byte[]? restored = await store.ReadBlobAsync(key);
```

Normally `RecoveryPersistence` builds these writes. Arbitrary text is not a valid manifest.

### LightSpace.Catalog

Validated serialization, migration, search/filter/sort, copy families, manifests and XMP. Schema 6 accepts catalogs 1–5 and hydrates sources from validated masters. Native XMP retains processing, not family identity. Depends on Core; no UI.

```sh
dotnet add package LightSpace.Catalog --prerelease
```

**Key types:** `CatalogSerializer` (Serialize/Deserialize/Validate/SerializeSettings/DeserializeSettings), `VirtualCopyCatalog`, `PhotoQuery`, `PhotoSort`, **`PhotoKindFilter`**, `XmpSidecar`, `XmpImportResult`, `XmpExportResult`, `RecoveryManifest`, `RecoverySourceReference`.

```csharp
using LightSpace.Catalog;
using LightSpace.Core;

CatalogDocument catalog = CatalogSerializer.Deserialize(File.ReadAllText("catalog.json"));
var picks = new PhotoQuery(Text: "alps", MinimumRating: 3,
    Flag: PhotoFlag.Pick, Sort: PhotoSort.Rating).Execute(catalog);
var copies = new PhotoQuery { Kind = PhotoKindFilter.VirtualCopies }.Execute(catalog);
if (picks.Count > 0)
{
    var photo = picks[0];
    XmpImportResult imported = XmpSidecar.Import(File.ReadAllText("IMG_0042.xmp"), photo.State);
    foreach (var warning in imported.Warnings) Console.WriteLine(warning);
    // Interactive hosts should present warnings and apply through EditorSession.
    photo.State = imported.State;
    File.WriteAllText("IMG_0042.xmp", XmpSidecar.Export(photo.State).Xml);
}
File.WriteAllText("catalog.json", CatalogSerializer.Serialize(catalog));
```

### LightSpace.Imaging

JPEG, PNG, WebP, BMP and first-GIF-frame decoding, EXIF orientation, downscaled sRGB previews and procedural landscapes. Limits: 64 MiB encoded, 100 megapixels decoded. Depends on Core/SkiaSharp; no UI.

```sh
dotnet add package LightSpace.Imaging --prerelease
```

**Key types:** `PhotoCodec.Import(name, bytes)`, `PhotoCodec.Decode(bytes, maxDimension)`, `SamplePhotos.CreateCatalog()`, `SamplePhotos.Create(seed)`.

```csharp
using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;

PhotoDocument photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
using SKImage preview = PhotoCodec.Decode(photo.Original, maxDimension: 1024);
CatalogDocument samples = SamplePhotos.CreateCatalog();
```

### LightSpace.Rendering.Skia

Development, curves, grading, masks, clones and color ranges use SkSL effects. Optical presentation composes with development; projective geometry is a draw matrix. Backend execution is host-dependent GPU/software. Histograms, Auto tone, bounded sampling and export are included. Matching immutable source arrays share decoding independently of processing. Depends on Imaging; no UI framework.

```sh
dotnet add package LightSpace.Rendering.Skia --prerelease
```

**Key types:** `PhotoRenderer` (Prepare/Draw/CreateShader/Export/CalculateHistogram/Auto/SampleSource/ReleasePhoto/Statistics/Presentation), `Histogram`, `PhotoTransform`, `GeometryMapping`, `ClippingIndicators`, `ToneLookupCache`, `BrushCoverageCache`, `RendererStatistics` with its six-field API plus CachedSources/SharedSourceHits.

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Rendering.Skia;
using SkiaSharp;

var photo = PhotoCodec.Import("architecture.jpg", File.ReadAllBytes("architecture.jpg"));
var session = new EditorSession(new CatalogDocument { Photos = [photo], ActivePhoto = photo.Id });
session.Edit("Correct perspective and contrast", state => state with
{
    Develop = state.Develop with
    {
        Exposure = .3f,
        Channels = new ChannelCurves
        {
            Master = new PointCurve { Points = [new(0, 0), new(.25f, .18f), new(.75f, .82f), new(1, 1)] }
        }
    },
    Geometry = new GeometrySettings { Vertical = -18, Rotate = 1.5f, ConstrainCrop = true },
    Optics = new LensCorrectionSettings(Distortion: 12, Vignetting: 8)
});
using var renderer = new PhotoRenderer();
File.WriteAllBytes("architecture-edited.jpg",
    renderer.Export(photo, SKEncodedImageFormat.Jpeg, quality: 92, maxDimension: 4096));
session.Undo(); // Original bytes remain untouched.
```

### LightSpace.Editing

`EditorSession` coalesces completed gestures into transactions. Previews change visible state; commit adds history and cancel restores the opening state. Versions, selective sync, frozen references, copy deltas and committed-revision recovery are included. Depends on Catalog/Storage; no UI.

```sh
dotnet add package LightSpace.Editing --prerelease
```

**Key types:** `EditorSession` (Catalog/Active/Selection/Edit/BeginGesture/Preview/CommitGesture/CancelGesture/Undo/Redo/SaveVersion/SyncSelected/ApplySettings/CreateAlbum/Changed), CreateVirtualCopies/RenameVirtualCopy/RemoveVirtualCopies operations, `ReferencePhotoSnapshot`, `SurveySelection`, `RecoveryPersistence`, `RecoveryCoordinator`.

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Storage;

var session = new EditorSession(catalog);
session.Changed += () => Console.WriteLine($"Revision {session.Revision}");
session.BeginGesture();
foreach (var value in new[] { .1f, .2f, .35f })
    session.Preview(state => state with { Develop = state.Develop with { Exposure = value } });
session.CommitGesture("Exposure");
session.SaveVersion("Bright");
session.SyncSelected(); // All processing; target metadata retained.
session.Undo();
session.SyncSelected(EditSettingsGroup.Light | EditSettingsGroup.ColorGrading);
var copy = session.CreateVirtualCopies([session.Active!.Id]).Single();
session.RenameVirtualCopy(copy.Id, "Warm alternative");
session.Edit("Alternative exposure", state => state with
{
    Develop = state.Develop with { Exposure = .8f }
});
session.RemoveVirtualCopies([copy.Id]);
session.Undo(); // Same copy identity/settings; original never deleted.
var persistence = new RecoveryPersistence(new FileRecoveryStore("recovery"));
using var recovery = RecoveryCoordinator.Incremental(session, persistence, initiallySaved: false);
await recovery.FlushAsync();
```

### LightSpace.Controls

Original Uno active/reference/Survey views, settings selection, sliders, curves, grading/mixer/range/mask editors, optics/geometry, interactive histogram, panel grips, thumbnails and chrome. Editors expose preview/commit/cancel without owning transaction policy. Brush settings affect new strokes. Depends on Editing/Rendering.Skia; requires Uno's Skia renderer.

```sh
dotnet add package LightSpace.Controls --prerelease
```

**Key types:** `PhotoViewport`, `ReferencePhotoView`, `PhotoSurveyView`, `SurveyCard`, `SettingsTransferEditor`, `AdjustmentSlider`, `PointCurveEditor`, `ColorGradingEditor`, `ColorMixerEditor`, `ColorRangeEditor`, `MaskSettingsEditor`, `GeometryEditor`, `OpticsEditor`, `BrushSettingsEditor`, `HistogramView`, `PanelResizeGrip`, `PhotoCard`, `PhotoThumbnail`, `ThumbnailCache`, `PanelSection`, `LightButton`, `Theme`.

```csharp
using LightSpace.Controls;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

var session = new EditorSession(catalog);
var renderer = new PhotoRenderer();
var viewport = new PhotoViewport(session, renderer);
var exposure = new AdjustmentSlider("Exposure", -5, 5, 0, .01);
exposure.ValueChanged += value => session.Preview(s => s with { Develop = s.Develop with { Exposure = value } });
exposure.ValueCommitted += () => session.CommitGesture("Exposure");
exposure.GestureCanceled += session.CancelGesture;
var curves = new PointCurveEditor { Value = session.Active!.State.Develop.Channels };
curves.Previewed += value => session.Preview(s => s with { Develop = s.Develop with { Channels = value } });
curves.Committed += () => session.CommitGesture("RGB point curve");
curves.Canceled += session.CancelGesture;
var geometry = new GeometryEditor { Value = session.Active.State.Geometry };
geometry.Previewed += value => session.Preview(s => s with { Geometry = value });
geometry.Committed += () => session.CommitGesture("Geometry");
geometry.Canceled += session.CancelGesture;
session.ViewChanged += () =>
{
    if (session.Active is not { } active) return;
    exposure.Value = active.State.Develop.Exposure;
    curves.Value = active.State.Develop.Channels;
    geometry.Value = active.State.Geometry;
};
var inspector = new StackPanel { Width = 300, Children = { exposure, curves, geometry } };
var root = new Grid { Background = Theme.Background };
root.ColumnDefinitions.Add(new ColumnDefinition());
root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
root.Children.Add(viewport);
var scroll = new ScrollViewer { Content = inspector };
Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
window.Content = root;
window.Closed += (_, _) => { viewport.Dispose(); renderer.Dispose(); };
```

See [reference embedding](docs/REFERENCE-AND-SYNC.md#reusable-model-and-ui-apis) and [Survey](docs/SURVEY.md) for multi-view wiring and releasing session-local processing identities. A standalone view does not dispose its external renderer.

### LightSpace.Workbench

Complete workspace control with catalog/grid/filmstrip, editing/optics/geometry, references, Survey, copy manager, settings transfer, tools, versions, dialogs, shortcuts, resizing and recovery. Depends on Controls/Storage; requires Uno. `ISidecarStorage` is optional.

```sh
dotnet add package LightSpace.Workbench --prerelease
```

**Key types:** `StudioView(EditorSession, IWorkspaceStorage, recoveryLoaded, RecoveryPersistence?)` exposes Session/Viewport/Recovery/SetGrid/ChooseTool/SetStatus/SaveRecoveryAsync, UnsavedChangesChanged, opt-in DiagnosticsChanged and ProtectExistingRecovery.

```csharp
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Storage;
using LightSpace.Workbench;

IWorkspaceStorage storage = new MyWorkspaceStorage(); // Host picker/export/recovery adapter.
var persistence = storage is IRecoveryStore store ? new RecoveryPersistence(store) : null;
var catalog = persistence is null ? null : await persistence.RestoreAsync();
var studio = new StudioView(new EditorSession(catalog ?? SamplePhotos.CreateCatalog()),
    storage, recoveryLoaded: catalog is not null, persistence: persistence);
window.Content = studio;
window.Closed += (_, _) => studio.Dispose();
```

Catch restore failures and protect unreadable prior data rather than replacing it automatically; the supplied application host implements that safeguard.

## Stack and rendering

Pinned: **.NET SDK 10.0.401**, **Uno SDK 6.7.30**, matched **SkiaSharp 3.119.4** managed/native assets. Upgrade graphics dependencies together and retain ABI/lifetime validation.

`SKCanvasElement` integrates composed effects in Uno's Skia path. Hardware execution requires a GPU-backed canvas; software remains supported. This is **not a separate WebGPU engine** or GPU-only import/export pipeline.

Decode, initial hashing, manifest serialization, brush texture publication, histogram and encoding retain CPU/native work. Preview targets are 2560 pixels for editing, 384 for thumbnails, 1024 for Survey and brush coverage, with output capped at 8192. Budgets bound retained buffers, not all transient/GPU memory. Channel alignment can evaluate development three times. Copies/reference views share sources within a renderer; distinct visible looks still shade separately. [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Ownership](docs/REFERENCE-AND-SYNC.md#shared-source-cache-and-gpu-work)

## Build and run

Install the pinned SDK, Python 3 and your OS's Uno desktop prerequisites.

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

Open `http://127.0.0.1:4173/LightSpace/`. Serve Uno/.NET WebAssembly over HTTP(S), not as a local HTML file. The build verifies the OFL font and optionally downloads demo photos. `LIGHTSPACE_NO_DEMO_PHOTOS=1` uses original generated landscapes.

## Tests and workflows

Build runs engine checks, publishes WebAssembly, drives real controls, verifies export/recovery and packages libraries. It retains screenshots, traces on failure, source, structured results and scoped performance counters. Desktop compiles Windows/Linux/macOS. Pages verifies the successful trusted main artifact's commit and repeats browser acceptance publicly.

The fatal-crash regression sweeps all 17 development sliders, then verifies undo/redo, JPEG decoding, final catalog values and reload. Photography, reference, Survey and copy tests check pixels, input, ownership, removal, metadata, shared sources and persistence. No editing feature or fatal-error reporting is disabled.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/*.png artifacts/fixtures/
npm run test:browser
npx playwright test slider-stability.spec.mjs
npx playwright test virtual-copies.spec.mjs virtual-copy-selection.spec.mjs
python3 -m unittest discover -s tests/scripts -v
python3 scripts/validate-packages.py --commit "$(git rev-parse HEAD)"
```

The package gate rejects missing/mixed pairs, wrong versions/commits, inconsistent internal dependencies, missing payloads and incomplete metadata. It records SHA-256 and checks ZIP CRCs without executing assemblies. Optional `--version`, `--directory`, `--output` support independent audits. This is not installed execution or proof of publication.

Release retains six-RID single-file packaging, browser archives, symbols and checksums. Tags create GitHub releases and publish with OIDC Trusted Publishing through the protected `nuget` environment. **Manual runs are dry runs** that build/upload artifacts only. A source commit implies neither tag nor publication. [Release contract](docs/RELEASES.md)

CI uses Chromium/SwiftShader, not physical GPUs or certified pen hardware. CPU timings, memory capacity and counters are scoped evidence, not universal speed/leak-free claims. Signing/notarization, installers and updates remain unconfigured.

## Compatibility and safety

Catalog **6** preserves copy families; versions 1–5 migrate as original-only records. Native XMP remains **5**, accepting 3–4, and records processing rather than family identity. Recovery manifest **1** and IndexedDB **2** are unchanged. Older builds reject schema 6. Retain pre-upgrade portable backups and reload; do not delete site data.

Limits: 64 MiB per source, 100 megapixels decoded, 256 MiB catalog source bytes and 5,000 records including copies. Output is 8-bit sRGB, at most 8192 pixels, without source EXIF/IPTC. Recovery is local/unencrypted with no tab merge or orphan cleanup. Keep source files and portable backups.

RAW/DNG/HEIF/TIFF, AI tools, calibrated profiles, automatic Upright, continuous crop aspect locking, HDR/panorama, native-resolution tiling, indexed durable catalogs, master promotion/stacks, printing/proofing and cloud workflows remain unimplemented. UI is Lightroom-inspired, not full pixel parity. [Feature ledger](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Virtual copies](docs/VIRTUAL-COPIES.md) · [Survey](docs/SURVEY.md) · [Reference/settings](docs/REFERENCE-AND-SYNC.md) · [Optics/geometry](docs/OPTICS-GEOMETRY.md) · [Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Slider hotfix](docs/WASM-SLIDER-FIX.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

Source is [MIT licensed](LICENSE). Dependencies, OFL font and optional Unsplash photos retain their licenses; photos are not MIT-relicensed. Adobe and Lightroom are comparative workflow references and trademarks of their owners. No affiliation, endorsement or proprietary catalog compatibility is claimed.
