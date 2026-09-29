<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · GPU-capable Skia effects · Reusable C# libraries

[Open LightSpace](https://wieslawsoltes.github.io/LightSpace/) · [Guide](docs/GETTING-STARTED.md) · [Advanced editing & XMP](docs/ADVANCED-EDITING.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Core.svg?label=NuGet)](https://www.nuget.org/packages/LightSpace.Core)
[![Downloads](https://img.shields.io/nuget/dt/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core)

</div>

---

LightSpace is a shared Uno desktop/WebAssembly photography application: browse a local catalog, develop a photograph, refine its composition and masks, and export a rendered copy while retaining the original bytes. The dark workspace combines a library sidebar, centered photo canvas, filmstrip, histogram, development inspector and vertical tool rail.

**Current version: 0.4.1-alpha.1.** This is independent, functional early-stage software—not a pixel-identical or feature-complete Adobe Lightroom replacement. No Adobe artwork, camera profiles, proprietary processing code or cloud services are included. [Implemented behavior and remaining boundaries →](docs/FEATURE-COVERAGE.md)

## 0.4.1 stability hotfix

Fixes the reproduced fatal WebAssembly failure during repeated edit-slider changes. Compiled shader output now has explicit ownership independent of native staging cleanup; uniform and child resources are deterministically released. Rendering features remain enabled.

The regression suite drives all 17 development sliders through extremes and neutral crossings, verifies one transaction per gesture, decodes an exported JPEG, checks every saved adjustment, and reloads recovery. Native tests force finalization and replay deferred draws after cache disposal. CI and Pages preserve one combined report including the stress test. [Investigation, evidence and limits](docs/WASM-SLIDER-FIX.md)

Reload an old or crashed browser tab after deployment. **Do not clear site data** to load this fix: existing catalog schema 4 and recovery database version 2 are unchanged. `build-info.json` identifies the loaded version and commit.

## Photography workflow

**Organize.** Import JPEG, PNG, WebP, BMP or GIF sources. Browse the paged grid/filmstrip, search names/captions/keywords, assign ratings and flags, and collect references in albums. Control-click extends the selection. Copy/paste and synchronization apply development settings across selected photographs.

**Develop.** Adjust light, relative white balance, vibrance/saturation, monochrome, curves and eight color bands. Four-way grading controls shadows, midtones, highlights and global color with blending and balance. Creative presets, texture/clarity/dehaze approximations, sharpening, spatial smoothing, vignette and grain provide additional tools.

**Refine.** Drag crop rectangles and handles, choose centered ratios, rotate in quarter turns and flip. Work with radial/linear gradients, sampled color/luminance ranges and brush masks. Local exposure, contrast, temperature, tint and saturation remain non-destructive. Clone stamps use an explicit source; comparison has a draggable divider.

**Preserve.** Each completed gesture is one transaction. Undo/redo, named versions, selected-photo synchronization, catalog backups and recovery preserve the settings. Export JPEG/PNG/WebP copies, ZIP a selection or create an XMP sidecar. Recovery acknowledges committed revisions only and protects unreadable prior data from automatic replacement.

## Advanced editing and recovery

**Arbitrary RGB curves.** Edit master, red, green and blue curves with up to 32 points each. Add/drag/delete points, enter numeric values, choose linear or shape-preserving smooth interpolation, and undo each gesture. A separate floating-point lookup cache avoids rebuilding curves when unrelated adjustment values change.

**Freehand masks.** Paint and erase with size, feather, flow, density and pen-pressure inputs. Arc-length resampling avoids pointer-event-dependent stroke density. Add/subtract brush coverage from existing analytic masks or create a new brush mask. Incremental coverage caching processes appended dabs rather than replaying the whole stroke on every update. Local adjustment changes reuse coverage.

**Sampled color selections.** Select up to five source colors with click/Shift-click, remove pins with Alt-click, and refine tolerance and smoothness. A reusable color-range editor supports standalone masks and intersections with gradient, luminance or brush coverage. The original Oklab-based selection runs in the existing Skia effect and is evaluated before development, without feeding a local correction back into its own selection.

**XMP sidecars.** Import standard metadata and an explicit Camera Raw parameter/curve subset with a compatibility report before applying it. Metadata-only import preserves processing. Export standard metadata, supported development values and an optional native extension for complete LightSpace settings round trips. Unknown Adobe processing is reported, not claimed as equivalent.

**Content-addressed recovery.** Immutable originals are stored separately under SHA-256 keys. Warm metadata saves serialize the edit manifest without rewriting, reading or rehashing unchanged source blobs. Browser commits atomically publish the manifest and newly staged blobs; native storage stages sources before replacing the manifest. Source integrity is checked on restore, failed commits remain dirty, and explicit retry can restage missing originals. Portable catalog exports still embed originals.

[Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Color selections](docs/COLOR-AND-MASKS.md) · [Recovery contract](docs/RECOVERY.md) · [Performance evidence](docs/PERFORMANCE.md)

## Download

Every [release](https://github.com/wieslawsoltes/LightSpace/releases) ships a self-contained, single-file desktop app — no .NET install needed:

| OS | x64 | Arm64 |
| --- | --- | --- |
| Windows | `LightSpace-<version>-win-x64.zip` | `LightSpace-<version>-win-arm64.zip` |
| macOS | `LightSpace-<version>-osx-x64.tar.gz` | `LightSpace-<version>-osx-arm64.tar.gz` |
| Linux | `LightSpace-<version>-linux-x64.tar.gz` | `LightSpace-<version>-linux-arm64.tar.gz` |

Extract and run `LightSpace` (`LightSpace.exe` on Windows). Builds are not code-signed yet: on macOS clear the quarantine flag with `xattr -d com.apple.quarantine LightSpace`; on Windows choose **More info → Run anyway** in SmartScreen. Verify downloads against `SHA256SUMS.txt`.

## NuGet packages

All eight libraries are MIT-licensed and published on [NuGet.org](https://www.nuget.org/packages?q=LightSpace). The six engine packages (Core, Storage, Catalog, Imaging, Rendering.Skia, Editing) target `net10.0` and do not depend on Uno; Imaging and Rendering.Skia use SkiaSharp 3.119. `LightSpace.Controls` and `LightSpace.Workbench` target `net10.0-desktop` and `net10.0-browserwasm` on Uno Platform with the Skia renderer. All packages are versioned together (currently prereleases), and symbols ship on NuGet.org as `.snupkg` with SourceLink.

```sh
dotnet add package LightSpace.Core --prerelease
```

| Package | Version | Downloads | Description |
| --- | --- | --- | --- |
| [LightSpace.Core](https://www.nuget.org/packages/LightSpace.Core) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | Immutable photo state, develop settings, grading, curves, masks/brush strokes, geometry, presets and catalog models |
| [LightSpace.Storage](https://www.nuget.org/packages/LightSpace.Storage) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | Platform-independent import/export contracts, an atomic content-addressed recovery store and an optional sidecar picker |
| [LightSpace.Catalog](https://www.nuget.org/packages/LightSpace.Catalog) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | Versioned catalog serialization and migration, photo queries, recovery manifests and XMP sidecar interchange |
| [LightSpace.Imaging](https://www.nuget.org/packages/LightSpace.Imaging) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | Bounded image decoding, EXIF orientation, sRGB conversion and original procedural sample photos |
| [LightSpace.Rendering.Skia](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | GPU-capable SkSL photo development, masks, clone spots, curve tables, histograms and crop-aware export |
| [LightSpace.Editing](https://www.nuget.org/packages/LightSpace.Editing) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | Non-destructive editing sessions, gesture transactions, undo/redo, versions, synchronization and revision-aware recovery |
| [LightSpace.Controls](https://www.nuget.org/packages/LightSpace.Controls) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | Original Uno photography controls: chrome, icons, sliders, grading/mixer/curve/mask/brush editors, thumbnails and the photo canvas |
| [LightSpace.Workbench](https://www.nuget.org/packages/LightSpace.Workbench) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | Composable photography workspace with a local catalog, inspectors, commands, dialogs and save/recovery UX |

Dependencies: `Core ← Catalog, Imaging`; `Imaging ← Rendering.Skia`; `Catalog + Storage ← Editing`; `Editing + Rendering.Skia ← Controls`; `Controls + Storage ← Workbench`. `Storage` has no dependencies. The host supplies platform storage and startup. Standard Uno text-input, scrolling, selection and accessibility primitives remain where appropriate; not every primitive is a newly implemented control.

Dispose workspaces and native-resource caches (`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport`, `StudioView`). Treat snapshot arrays as copy-on-write and original bytes as read-only; mutation in place violates the cache contract. Objects are confined to one logical owner, normally the UI synchronization context. [Component API](docs/ADVANCED-EDITING.md) · [Recovery invariants](docs/RECOVERY.md)

### LightSpace.Core

The immutable editing model: `PhotoState` (develop settings, crop, local masks, clone spots, rating, flag, keywords), point curves with linear or shape-preserving interpolation, four-way color grading, sampled color ranges, brush strokes, built-in presets and the `CatalogDocument` of photos, versions and albums. Every record has a `Normalize()` that clamps values into range. No dependencies and no UI.

```sh
dotnet add package LightSpace.Core --prerelease
```

**Key types**
- `PhotoState`, `DevelopSettings` (17 sliders plus `Get`/`Set` by name), `CropSettings`, `CloneSpot`.
- `PointCurve` / `ChannelCurves` / `CompiledPointCurve`: up to 32 points per channel, `Compile().Evaluate(x)`.
- `ColorGradingSettings` / `GradingTone` and `ColorBand` (eight-band mixer).
- `LocalMask` (`Radial`, `Linear`, `LuminanceRange`, `Brush`, `ColorRange`) with `ColorRangeSettings`, `BrushStroke` and `BrushStrokeBuilder`.
- `CatalogDocument`, `PhotoDocument`, `Album`, `NamedVersion`, `BuiltInPresets.All`.

**Usage**

```csharp
using LightSpace.Core;

var state = new PhotoState
{
    Develop = BuiltInPresets.All.First(p => p.Name == "Golden hour").Settings with { Exposure = .3f },
    Crop = new CropSettings(Left: .05f, Top: .05f, Right: .95f, Bottom: .9f),
    Masks = [new LocalMask { Name = "Sky", Kind = MaskKind.Linear, Y = .2f, Exposure = -.4f }],
    Rating = 4,
    Flag = PhotoFlag.Pick,
    Keywords = ["alps", "sunset"]
}.Normalize();                          // clamps every value into its valid range

var develop = state.Develop.Set(nameof(DevelopSettings.Contrast), 20);
var curve = new PointCurve { Points = [new(0, 0), new(.25f, .18f), new(.75f, .82f), new(1, 1)] };
float midtone = curve.Compile().Evaluate(.5f);
(int width, int height) = state.Crop.OutputSize(6000, 4000);
```

### LightSpace.Storage

Contracts the host implements for pickers, downloads and recovery, plus `FileRecoveryStore`, a native implementation that stores immutable originals under SHA-256 keys and atomically replaces the edit manifest only after every referenced source is staged. No dependencies and no UI.

```sh
dotnet add package LightSpace.Storage --prerelease
```

**Key types**
- `IWorkspaceStorage`: `OpenImagesAsync`, `OpenCatalogAsync`, `SaveAsync`, `ReadRecoveryAsync`, `WriteRecoveryAsync`; `WorkspaceFile(Name, Bytes)`.
- `IRecoveryStore`: `ReadManifestAsync`, `ReadBlobAsync`, `CommitAsync(RecoveryWrite)`; implemented by `FileRecoveryStore(directory)`.
- `RecoveryWrite(Manifest, References, Blobs)`, `RecoveryBlob(Key, Bytes)`, `RecoveryKeys` (`Hash`, `Validate`, size limits).
- `ISidecarStorage`: optional, user-authorized XMP sidecar picker (no implicit folder scanning).

**Usage**

```csharp
using LightSpace.Storage;

IRecoveryStore store = new FileRecoveryStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "recovery"));

byte[] original = File.ReadAllBytes("mountains.jpg");
string key = RecoveryKeys.Hash(original);   // SHA-256, lowercase hex
await store.CommitAsync(new RecoveryWrite(manifestJson, [key], [new RecoveryBlob(key, original)]));

string? manifest = await store.ReadManifestAsync();
byte[]? restored = await store.ReadBlobAsync(key);
```

In practice `RecoveryPersistence` (LightSpace.Editing) builds these writes for you.

### LightSpace.Catalog

Catalog persistence and interchange: validated JSON serialization with schema 1–4 migration and size limits, search/filter/sort queries, the recovery manifest format, and XMP sidecar import/export (standard metadata, a documented Camera Raw subset and an optional native extension for exact LightSpace round trips). Depends on Core; no UI.

```sh
dotnet add package LightSpace.Catalog --prerelease
```

**Key types**
- `CatalogSerializer`: `Serialize`, `Deserialize`, `Validate`, `SerializeSettings`/`DeserializeSettings`.
- `PhotoQuery(Text, MinimumRating, Flag, Album, Sort).Execute(catalog)` with `PhotoSort`.
- `XmpSidecar.Import(xml, baseline, options)` returns `XmpImportResult` (state, applied properties, warnings); `XmpSidecar.Export(state, ...)` returns `XmpExportResult`.
- `RecoveryManifest` and `RecoverySourceReference`.

**Usage**

```csharp
using LightSpace.Catalog;
using LightSpace.Core;

CatalogDocument catalog = CatalogSerializer.Deserialize(File.ReadAllText("catalog.json")); // validated, schemas 1–4
var picks = new PhotoQuery(Text: "alps", MinimumRating: 3, Flag: PhotoFlag.Pick, Sort: PhotoSort.Rating).Execute(catalog);

var photo = picks[0];
XmpImportResult imported = XmpSidecar.Import(File.ReadAllText("IMG_0042.xmp"), photo.State);
foreach (var warning in imported.Warnings)
    Console.WriteLine(warning);         // unsupported Camera Raw values are reported
photo.State = imported.State;

File.WriteAllText("IMG_0042.xmp", XmpSidecar.Export(photo.State).Xml);
File.WriteAllText("catalog.json", CatalogSerializer.Serialize(catalog));
```

### LightSpace.Imaging

Bounded decoding for JPEG, PNG, WebP, BMP and GIF sources (64 MiB encoded, 100 megapixels decoded), EXIF orientation, downscaled sRGB previews and original procedural sample landscapes. Depends on Core and SkiaSharp; no UI.

```sh
dotnet add package LightSpace.Imaging --prerelease
```

**Key types**
- `PhotoCodec.Import(name, bytes)`: validates the source and returns a `PhotoDocument` with oriented dimensions and the original bytes.
- `PhotoCodec.Decode(bytes, maxDimension)`: an oriented, downscaled sRGB `SKImage`.
- `SamplePhotos.CreateCatalog()` / `SamplePhotos.Create(seed)`: generated demo content.

**Usage**

```csharp
using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;

// Validates size (64 MiB, 100 MP) and records EXIF-oriented dimensions; keeps the original bytes.
PhotoDocument photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));

// Oriented, downscaled sRGB preview.
using SKImage preview = PhotoCodec.Decode(photo.Original, maxDimension: 1024);

CatalogDocument samples = SamplePhotos.CreateCatalog(); // eight original procedural landscapes
```

### LightSpace.Rendering.Skia

The development pipeline as compiled SkSL runtime effects: light, color, curves (2048-entry floating-point lookup images), grading, mixer, local masks with incremental brush coverage, clone spots, sampled color ranges, histograms, auto settings and crop-aware JPEG/PNG/WebP export. Runs on whatever Skia backend the host provides (GPU or software). Depends on Imaging; no UI framework.

```sh
dotnet add package LightSpace.Rendering.Skia --prerelease
```

**Key types**
- `PhotoRenderer`: `Draw(canvas, photo, destination, ...)`, `CreateShader`, `Export`, `CalculateHistogram`, `Auto`, `SampleSource`, `Statistics`.
- `Histogram`: red/green/blue/luminance bins.
- `PhotoTransform`: `Fit` and `SourceToView` for crop-aware layout.
- `ToneLookupCache` / `BrushCoverageCache`: reusable caches with statistics.

**Usage**

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

### LightSpace.Editing

`EditorSession` turns every completed gesture into one transaction: previews update the active photo without creating history, commits push one undo step and cancels restore the pre-gesture state. It also provides named versions, synchronization across the selection, albums and revision-aware recovery that acknowledges committed revisions only. Depends on Catalog and Storage; no UI.

```sh
dotnet add package LightSpace.Editing --prerelease
```

**Key types**
- `EditorSession`: `Catalog`, `Active`, `Selection`, `Edit`, `BeginGesture`/`Preview`/`CommitGesture`/`CancelGesture`, `Undo`/`Redo`, `SaveVersion`, `SyncSelected`, `CreateAlbum`, `Changed`.
- `RecoveryPersistence(IRecoveryStore)`: `Capture`, `CommitAsync`, `RestoreAsync` without rewriting unchanged originals.
- `RecoveryCoordinator`: `FlushAsync` (concurrent calls join one write), `Status` (`RecoveryStatus`) and `StatusChanged`.

**Usage**

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Storage;

var session = new EditorSession(catalog);
session.Changed += () => Console.WriteLine($"Revision {session.Revision}");

// A slider drag: previews are not history entries; the commit is one undo step.
session.BeginGesture();
foreach (var value in new[] { .1f, .2f, .35f })
    session.Preview(state => state with { Develop = state.Develop with { Exposure = value } });
session.CommitGesture("Exposure");

session.SaveVersion("Bright");
session.SyncSelected();                 // copy develop/crop/masks to the selection
session.Undo();

// Persist committed revisions only (content-addressed originals + manifest).
var persistence = new RecoveryPersistence(new FileRecoveryStore("recovery"));
using var recovery = RecoveryCoordinator.Incremental(session, persistence);
await recovery.FlushAsync();
```

### LightSpace.Controls

Original, dark-themed Uno controls for photography apps: the interactive `PhotoViewport` (crop, radial/linear masks, brush, color sampling, clone, before/after comparison), adjustment sliders, curve, grading, mixer, color-range, mask and brush editors, histogram, thumbnails and chrome. Editors expose preview/commit/cancel contracts without owning the host's transaction policy; `BrushSettingsEditor` changes tool settings, not existing strokes. Depends on Editing and Rendering.Skia; requires Uno Platform (Skia renderer).

```sh
dotnet add package LightSpace.Controls --prerelease
```

**Key types**
- `PhotoViewport(EditorSession, PhotoRenderer)`: `SetTool(PhotoTool)`, `Fit`, `Compare`, `Before`, `MaskOverlay`, `SetActiveMask`, `BrushSettings`.
- `AdjustmentSlider(name, min, max, value, step)`: `ValueChanged`, `ValueCommitted`, `GestureCanceled`.
- `PointCurveEditor`, `ColorGradingEditor`, `ColorMixerEditor`, `ColorRangeEditor`, `MaskSettingsEditor`: `Value` plus `Previewed`/`Committed`/`Canceled`.
- `BrushSettingsEditor`, `HistogramView`, `PhotoCard`/`PhotoThumbnail` with `ThumbnailCache`, `PanelSection`, `LightButton`, `Theme`.

**Usage**

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

var inspector = new StackPanel { Width = 300, Children = { exposure, curves } };
var root = new Grid { Background = Theme.Background };
root.ColumnDefinitions.Add(new ColumnDefinition());
root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
Grid.SetColumn(inspector, 1);
root.Children.Add(viewport);
root.Children.Add(inspector);
window.Content = root;
```

### LightSpace.Workbench

The complete LightSpace workspace as one control: library sidebar and grid, filmstrip, histogram, develop inspector, tool rail, crop/mask/clone tools, presets, versions, XMP and export dialogs, keyboard shortcuts and recovery/save status. Depends on Controls and Storage; requires Uno Platform. `ISidecarStorage` is optional for embedded hosts.

```sh
dotnet add package LightSpace.Workbench --prerelease
```

**Key types**
- `StudioView(EditorSession, IWorkspaceStorage, recoveryLoaded, RecoveryPersistence?)`: `Session`, `Viewport`, `Recovery`, `SetGrid`, `ChooseTool`, `SetStatus`, `SaveRecoveryAsync`.
- Events: `UnsavedChangesChanged` (for unload prompts) and `DiagnosticsChanged` (`StudioDiagnostics`).
- `ProtectExistingRecovery(reason)`: keep unreadable prior recovery data from automatic replacement.

**Usage**

```csharp
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Storage;
using LightSpace.Workbench;

IWorkspaceStorage storage = new MyWorkspaceStorage();   // pickers, downloads, recovery
var persistence = storage is IRecoveryStore store ? new RecoveryPersistence(store) : null;
var catalog = persistence is null ? null : await persistence.RestoreAsync();

var studio = new StudioView(new EditorSession(catalog ?? SamplePhotos.CreateCatalog()),
    storage, recoveryLoaded: catalog is not null, persistence: persistence);
window.Content = studio;
window.Closed += (_, _) => studio.Dispose();
```

## Stack and rendering

Pinned versions are **.NET SDK 10.0.401**, **Uno SDK 6.7.30** and matched **SkiaSharp 3.119.4** managed/native assets. Graphics dependencies must be upgraded together and validated against Uno's ABI.

The viewport uses `Uno.WinUI.Graphics2DSK.SKCanvasElement` and compiled SkSL runtime effects within Uno's Skia composition path. Hardware execution depends on the host supplying a GPU-backed canvas. Software rendering remains available for deterministic tests and export. This release does **not** have a separate WebGPU compute backend.

CPU/native decode, manifest serialization, first-use source hashing, brush texture publication and export remain synchronous. Storage writes are asynchronous; warm manifest commits exclude original bytes. Viewport sources have a 2560-pixel preview target; thumbnails decode at 384 pixels; brush coverage is capped at 1024 pixels even during export. Curves use 2048-entry floating-point lookup images. Cache budgets bound retained buffers, not all transient allocations or GPU copies. [Architecture](docs/ARCHITECTURE.md) · [Performance methods](docs/PERFORMANCE.md)

## Build and run

Install the SDK in `global.json`, Python 3 and your operating system's Uno desktop prerequisites.

```bash
git clone https://github.com/wieslawsoltes/LightSpace.git
cd LightSpace
python3 scripts/fetch-assets.py
dotnet workload install wasm-tools --skip-manifest-update

# Model, processing, lifetime, recovery, XMP and performance checks
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

## Tests and workflows

`Build` runs the engine suite, publishes WebAssembly, exercises actual Uno controls with Playwright, verifies exports/recovery and packages all libraries. Reports include screenshots, failure traces, machine-readable test results, slider-stability evidence and performance counters. `Desktop` compiles Windows, Linux and macOS hosts. `Pages` deploys a successful trusted main build, checks commit provenance and repeats acceptance tests against the public URL. `Release` runs for `v*` tags or a supplied manual version. It runs the engine suite, publishes self-contained single-file desktop executables for Windows, macOS and Linux (x64 and arm64), builds the browser archive, packs the eight libraries with symbols and emits `SHA256SUMS.txt`. Tags attach all assets to a GitHub Release and publish the packages to NuGet.org with [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC, no stored API key) from the protected `nuget` environment. Manual runs are dry runs: they build and upload every asset as workflow artifacts but publish nothing.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/*.png artifacts/fixtures/
npm run test:browser

# Focused fatal-crash regression
npx playwright test slider-stability.spec.mjs
```

CI browser testing uses Chromium/SwiftShader. It is not physical-GPU or pen-hardware certification. Microbenchmarks, linear-memory capacity and work-avoidance counters are scoped evidence, not claims of universal application speedup or absence of every memory leak. Signing/notarization, installers and automatic updates are not configured.

## Compatibility and data safety

Catalog schema **4** preserves sampled colors, curves and brush data. Schemas 1–3 migrate with neutral defaults; older applications reject unsupported new files. The 0.4.1 hotfix does not change that schema or recovery layout. Keep pre-upgrade backups when old-version interoperability matters.

Import limits are 64 MiB per source, 100 megapixels decoded and 256 MiB of encoded originals per catalog. Image output is 8-bit sRGB with an 8192-pixel long-edge cap; source EXIF/IPTC is not embedded in rendered copies. XMP supports a documented subset, not Lightroom catalogs/profiles or pixel-equivalent Adobe development.

RAW/DNG/HEIF/TIFF, AI tools, calibrated camera/lens correction, HDR/panorama merging, arbitrary crop straightening, native-resolution tiled inspection, indexed durable catalog storage, printing/proofing and cloud synchronization remain unimplemented. Recovery is an unencrypted local manifest and source store, not a cross-tab merge system or a substitute for original-file backups. Browser database version 2 cannot be opened by older builds requesting version 1. [Complete boundary ledger](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Slider hotfix](docs/WASM-SLIDER-FIX.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

LightSpace source is [MIT licensed](LICENSE). Dependencies, the OFL font and optional Unsplash photographs retain their licenses; photos are not relicensed as MIT. Adobe and Lightroom are trademarks of their owners and are used only as comparative workflow references. No endorsement or proprietary catalog compatibility is claimed.
