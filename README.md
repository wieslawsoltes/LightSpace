<div align="center">

# LightSpace

### A local-first photography workspace for desktop and browser

Non-destructive editing · Custom Uno controls · Composed Skia effects · Reusable C# libraries

[Open LightSpace](https://wieslawsoltes.github.io/LightSpace/) · [Guide](docs/GETTING-STARTED.md) · [Reference & selective settings](docs/REFERENCE-AND-SYNC.md) · [Feature coverage](docs/FEATURE-COVERAGE.md)

[![Build](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/build.yml)
[![Desktop](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/desktop.yml)
[![Pages](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml/badge.svg)](https://github.com/wieslawsoltes/LightSpace/actions/workflows/pages.yml)
[![MIT](https://img.shields.io/badge/license-MIT-7aa7ca.svg)](LICENSE)

</div>

---

LightSpace is a shared Uno desktop/WebAssembly photography application: browse a local catalog, develop a photograph, refine its composition and masks, and export a rendered copy while retaining the original bytes. The dark workspace combines a library sidebar, centered photo canvas, filmstrip, histogram, development inspector and vertical tool rail.

**Current source version: 0.7.0-alpha.1.** This is independent, functional early-stage software—not pixel-identical or feature-complete Adobe Lightroom. No Adobe artwork, proprietary processing, calibrated camera/lens profiles or cloud services are included. [Implemented behavior and remaining boundaries →](docs/FEATURE-COVERAGE.md)

## New in 0.7

**Multi-photo Survey culling.** Survey view or N compares selected photos, or the filtered sequence when fewer than two are selected. Twelve photos fit each page, with aspect-aware rows, filename/rating/flag controls, keyboard navigation, reversible exclusions and rejected-photo hiding. Exclusion never deletes originals or catalog records. Ratings and flags target only the active candidate and remain undoable and recoverable.

**Bounded preparation and direct rendering.** A single Uno/Skia surface draws the page through the existing composed effects. A separate twelve-photo, 1024px preview renderer retains up to 48 MiB of decoded sources, avoiding a too-small cache cycling through a larger page. One photo is prepared per dispatcher tick. This amortizes synchronous CPU/native preparation; it is not GPU decoding or a frame-time guarantee. Metadata changes retain cards, layout and pixel caches. Leaving Survey clears its preview caches and source references.

**Reusable, tested workflows.** SurveyLayout, SurveySelection, targeted EditorSession edits, SurveyCard and PhotoSurveyView provide reusable layers. Engine and real-browser checks cover paging, exclusion safety, single-photo undo, warm cache reuse, compact layouts and recovery. Existing all-slider crash regressions remain enabled. [Survey guide, APIs and measured scopes](docs/SURVEY.md)

## Reference comparison from 0.6

**Frozen reference comparison.** Pin the current edited look beside the active photograph with Reference view or Shift+R. Keep editing or choose another filmstrip photo without changing the reference. Switch side-by-side/stacked layouts, link or unlink fit-relative navigation, fit both panes, pin again, or apply selected settings from the reference. The reference stays session-local and adds no catalog record or processing revision.

**Selective copy and synchronization.** Choose thirteen independent processing groups instead of copying every edit. All/None/Global presets, captured source/target review and one-transaction batch undo make transfer explicit. Ratings, flags, labels, captions, keywords, original bytes and unselected edits remain intact. Ctrl/Cmd+Shift+C opens group selection; ordinary copy retains the all-processing shortcut.

**Shared decoded sources, independent looks.** Both views draw directly through the composed Skia pipeline. Matching immutable source arrays share a decoded image while preserving separate development, presentation and geometry state. Closing a reference releases its processing identity without clearing active caches. Batch applications resolve targets once rather than scanning the entire catalog for every changed photo.

**Framing, dialogs and stability.** Linked normalized pan survives pane/window resizing. Short dialogs fit their content; long bodies scroll with reachable actions. Source-sharing lifetime, frozen pixels, group isolation, selective export, recovery and actual browser input are covered by regression tests. The 0.4.1 shader-lifetime repair and fatal-slider stress remain enabled. [Reference/settings behavior, APIs and performance scopes](docs/REFERENCE-AND-SYNC.md) · [Validation](docs/VALIDATION.md)

## Photography foundation from 0.5

Manual Optics corrects distortion, lens falloff and radial channel alignment. Geometry adjusts vertical/horizontal perspective, rotation, aspect, scale and offsets, with conservative constrained framing and drawn-horizon straightening. A source-patch white-balance picker, five-region histogram dragging, independent clipping indicators, panel resizing, filmstrip toggle and focus mode extend the workspace.

Development feeds a composed optical runtime effect directly, without a CPU-rendered intermediate. Projective geometry is a cached matrix. Picking and masks use inverse optical/projective mapping, retaining attachment to source coordinates. [Optics and geometry guide](docs/OPTICS-GEOMETRY.md)

Catalog schema 5, native XMP settings 5, recovery manifest 1 and IndexedDB version 2 are unchanged by 0.6 and 0.7. Reload an older browser tab after deployment. **Do not clear site data**: it contains local recovery. `build-info.json` identifies the deployed source version and commit. Keep pre-upgrade backups when using older processing schemas. [Crash-fix evidence](docs/WASM-SLIDER-FIX.md)

## Photography workflow

**Organize.** Import JPEG, PNG, WebP, BMP or GIF sources. Browse paged grid/detail/filmstrip views, search filenames/captions/keywords, assign ratings and flags, and collect references in albums. Control-click extends selection. Selective copy/paste and synchronization transfer processing while retaining target metadata.

**Develop.** Adjust tone, relative white balance, saturation/vibrance, monochrome and eight hue bands. Master/R/G/B point curves support up to 32 points, numeric/pointer/keyboard editing and linear or shape-preserving interpolation. Four-way grading controls shadows, midtones, highlights and global tint. Creative presets, detail/effect approximations, sharpening, spatial smoothing, grain and vignette provide further tools.

**Refine.** Use crop handles, centered ratio presets, quarter turns/flips, straightening and manual perspective. Create radial/linear gradients, luminance ranges or five-color Oklab selections. Paint/erase masks with distance-resampled dabs, feather, flow, density and supplied pen pressure. Local tone/color adjustments reuse brush coverage. Clone stamps use explicit source locations; before/after has a draggable divider and reference comparison retains a frozen look.

**Preserve.** Completed gestures are individual undo transactions. Named versions retain alternative looks. Export JPEG/PNG/WebP, ZIP selected photos, save a portable source-inclusive catalog, or exchange XMP sidecars. Native LightSpace XMP settings round-trip complete processing; the reported Camera Raw subset is not equivalent Adobe development.

**Stay local.** Revision-aware recovery stores originals by SHA-256 and publishes a separate committed edit manifest. Warm metadata saves avoid rewriting or rehashing unchanged originals. Restore verifies source integrity; failed writes remain dirty and unreadable recovery is protected from automatic replacement. No account, photo upload or cloud processing is required.

[Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Color selections](docs/COLOR-AND-MASKS.md) · [Recovery contract](docs/RECOVERY.md) · [Performance evidence](docs/PERFORMANCE.md)

## Download

The [release workflow](https://github.com/wieslawsoltes/LightSpace/releases) produces self-contained, single-file desktop applications; no separate .NET installation is needed. Select the actual published version and matching processor architecture:

| OS | x64 | Arm64 |
| --- | --- | --- |
| Windows | `LightSpace-<version>-win-x64.zip` | `LightSpace-<version>-win-arm64.zip` |
| macOS | `LightSpace-<version>-osx-x64.tar.gz` | `LightSpace-<version>-osx-arm64.tar.gz` |
| Linux | `LightSpace-<version>-linux-x64.tar.gz` | `LightSpace-<version>-linux-arm64.tar.gz` |

Extract and run `LightSpace` (`LightSpace.exe` on Windows). Native dependencies extract at startup. Builds are not code-signed or notarized; verify their source and `SHA256SUMS.txt` before opening them. A source version bump does not itself publish new release assets. [Packaging and publication](docs/RELEASES.md)

## NuGet packages

All eight libraries are MIT-licensed. The badges below show the versions and downloads on [NuGet.org](https://www.nuget.org/packages?q=LightSpace), independently of the current source version. The six engine packages (Core, Storage, Catalog, Imaging, Rendering.Skia, Editing) target `net10.0` without Uno; Imaging and Rendering.Skia use SkiaSharp 3.119. Controls and Workbench target `net10.0-desktop` and `net10.0-browserwasm` with the Uno Skia renderer. Packages are versioned together, with `.snupkg` symbols and SourceLink. Build artifacts alone do not imply that a new version has been published.

```sh
dotnet add package LightSpace.Core --prerelease
```

| Package | Version | Downloads | Description |
| --- | --- | --- | --- |
| [LightSpace.Core](https://www.nuget.org/packages/LightSpace.Core) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Core.svg)](https://www.nuget.org/packages/LightSpace.Core) | Photo state, curves, grading, masks, optical/projective geometry, selective processing groups and catalog models |
| [LightSpace.Storage](https://www.nuget.org/packages/LightSpace.Storage) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Storage.svg)](https://www.nuget.org/packages/LightSpace.Storage) | Import/export contracts, atomic content-addressed recovery and optional sidecar picking |
| [LightSpace.Catalog](https://www.nuget.org/packages/LightSpace.Catalog) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Catalog.svg)](https://www.nuget.org/packages/LightSpace.Catalog) | Versioned catalog serialization/migration, queries, recovery manifests and XMP interchange |
| [LightSpace.Imaging](https://www.nuget.org/packages/LightSpace.Imaging) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Imaging.svg)](https://www.nuget.org/packages/LightSpace.Imaging) | Bounded image decoding, EXIF orientation, sRGB conversion and procedural samples |
| [LightSpace.Rendering.Skia](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Rendering.Skia.svg)](https://www.nuget.org/packages/LightSpace.Rendering.Skia) | Composed effects, projective drawing, shared-source caches, masks, curves, histogram and export |
| [LightSpace.Editing](https://www.nuget.org/packages/LightSpace.Editing) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Editing.svg)](https://www.nuget.org/packages/LightSpace.Editing) | Transactions, undo/redo, frozen references, selective synchronization, versions and revision-aware recovery |
| [LightSpace.Controls](https://www.nuget.org/packages/LightSpace.Controls) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Controls.svg)](https://www.nuget.org/packages/LightSpace.Controls) | Original Uno editors, histogram, thumbnails, active/reference viewports and settings-group selection |
| [LightSpace.Workbench](https://www.nuget.org/packages/LightSpace.Workbench) | [![NuGet](https://img.shields.io/nuget/vpre/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | [![Downloads](https://img.shields.io/nuget/dt/LightSpace.Workbench.svg)](https://www.nuget.org/packages/LightSpace.Workbench) | Composable photography workspace, reference comparison, catalog UI, inspectors, dialogs and recovery UX |

Dependencies: `Core ← Catalog, Imaging`; `Imaging ← Rendering.Skia`; `Catalog + Storage ← Editing`; `Editing + Rendering.Skia ← Controls`; `Controls + Storage ← Workbench`. Storage has no project dependencies. The host supplies startup and platform storage. Standard Uno input, scrolling and accessibility primitives remain in use where appropriate.

Dispose workspaces and native-resource caches (`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport`, `StudioView`). Treat snapshot arrays as copy-on-write and originals as read-only. Objects have one logical owner, normally the UI synchronization context. [Component API](docs/ADVANCED-EDITING.md) · [Reference/settings API](docs/REFERENCE-AND-SYNC.md) · [Recovery invariants](docs/RECOVERY.md)

### LightSpace.Core

The editing model includes `PhotoState`, development/crop/optics/geometry, local masks, clone spots, ratings, flags and keywords. Point curves support linear or shape-preserving interpolation; grading, sampled color ranges, brush strokes and built-in presets are UI-independent. Settings normalize values to their supported ranges. Selective transfers apply only chosen processing groups. No dependencies and no UI.

```sh
dotnet add package LightSpace.Core --prerelease
```

**Key types**
- `PhotoState`, `DevelopSettings` (17 adjustments and named Get/Set), `CropSettings`, `CloneSpot`.
- `GeometrySettings`, `LensCorrectionSettings`, `GeometryProjection`, `ProjectiveTransform`, `LensMapping`, `WhiteBalanceEstimator`.
- `PointCurve`, `ChannelCurves`, `CompiledPointCurve`, `ColorGradingSettings`, `GradingTone`, `ColorBand`.
- `LocalMask`, `ColorRangeSettings`, `BrushStroke`, `BrushStrokeBuilder`.
- `CatalogDocument`, `PhotoDocument`, `Album`, `NamedVersion`, `BuiltInPresets`.
- `EditSettingsGroup`, `EditSettingsTransfer`, `PhotoNavigationState`.

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

Host contracts for pickers, downloads and recovery. `FileRecoveryStore` stages immutable originals under SHA-256 keys and atomically replaces the manifest after required sources exist. No project dependencies or UI.

```sh
dotnet add package LightSpace.Storage --prerelease
```

**Key types**
- `IWorkspaceStorage`: OpenImagesAsync, OpenCatalogAsync, SaveAsync, ReadRecoveryAsync, WriteRecoveryAsync; `WorkspaceFile(Name, Bytes)`.
- `IRecoveryStore`: ReadManifestAsync, ReadBlobAsync, CommitAsync; `FileRecoveryStore(directory)`.
- `RecoveryWrite(Manifest, References, Blobs)`, `RecoveryBlob(Key, Bytes)`, `RecoveryKeys`.
- `ISidecarStorage`: optional user-authorized XMP picker.

```csharp
using LightSpace.Storage;

IRecoveryStore store = new FileRecoveryStore(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "recovery"));

// manifestJson must describe these sources using the recovery manifest contract.
byte[] original = File.ReadAllBytes("mountains.jpg");
string key = RecoveryKeys.Hash(original);
await store.CommitAsync(new RecoveryWrite(manifestJson, [key], [new RecoveryBlob(key, original)]));
string? manifest = await store.ReadManifestAsync();
byte[]? restored = await store.ReadBlobAsync(key);
```

Normally `RecoveryPersistence` in LightSpace.Editing builds these writes for the host.

### LightSpace.Catalog

Validated catalog serialization, migration, search/filter/sort, recovery manifests and XMP metadata/settings interchange. Current schema 5 accepts catalogs 1–4 with neutral missing fields. The Camera Raw subset is reported explicitly; native extensions retain complete LightSpace state. Depends on Core; no UI.

```sh
dotnet add package LightSpace.Catalog --prerelease
```

**Key types**
- `CatalogSerializer`: Serialize, Deserialize, Validate, SerializeSettings, DeserializeSettings.
- `PhotoQuery(Text, MinimumRating, Flag, Album, Sort).Execute(catalog)` and `PhotoSort`.
- `XmpSidecar`, `XmpImportResult`, `XmpExportResult`, `RecoveryManifest`, `RecoverySourceReference`.

```csharp
using LightSpace.Catalog;
using LightSpace.Core;

CatalogDocument catalog = CatalogSerializer.Deserialize(File.ReadAllText("catalog.json"));
var picks = new PhotoQuery(Text: "alps", MinimumRating: 3,
    Flag: PhotoFlag.Pick, Sort: PhotoSort.Rating).Execute(catalog);
if (picks.Count > 0)
{
    var photo = picks[0];
    XmpImportResult imported = XmpSidecar.Import(File.ReadAllText("IMG_0042.xmp"), photo.State);
    foreach (var warning in imported.Warnings) Console.WriteLine(warning);
    // An interactive host should present warnings before applying this state.
    photo.State = imported.State;
    File.WriteAllText("IMG_0042.xmp", XmpSidecar.Export(photo.State).Xml);
}
File.WriteAllText("catalog.json", CatalogSerializer.Serialize(catalog));
```

### LightSpace.Imaging

Bounded decoding for JPEG, PNG, WebP, BMP and GIF sources, EXIF orientation, downscaled sRGB previews and procedural sample landscapes. Limits are 64 MiB encoded and 100 megapixels decoded. Depends on Core and SkiaSharp; no UI.

```sh
dotnet add package LightSpace.Imaging --prerelease
```

**Key types**
- `PhotoCodec.Import(name, bytes)` validates source metadata and retains original bytes.
- `PhotoCodec.Decode(bytes, maxDimension)` returns an oriented sRGB `SKImage`.
- `SamplePhotos.CreateCatalog()` and `SamplePhotos.Create(seed)` provide generated content.

```csharp
using LightSpace.Core;
using LightSpace.Imaging;
using SkiaSharp;

PhotoDocument photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
using SKImage preview = PhotoCodec.Decode(photo.Original, maxDimension: 1024);
CatalogDocument samples = SamplePhotos.CreateCatalog();
```

### LightSpace.Rendering.Skia

Development, curves, grading, masks, clone spots and color ranges run as SkSL runtime effects. A separate optical effect composes with development; projective geometry is a draw matrix. The backend is selected by the host and may be GPU or software. Histograms, Auto settings, source sampling and JPEG/PNG/WebP export are also provided. Matching source buffers share decoding independently of per-photo shader state. Depends on Imaging; no UI framework.

```sh
dotnet add package LightSpace.Rendering.Skia --prerelease
```

**Key types**
- `PhotoRenderer`: Draw, CreateShader, Export, CalculateHistogram, Auto, SampleSource, ReleasePhoto, Statistics and Presentation.
- `Histogram`, `PhotoTransform`, `GeometryMapping`, `ClippingIndicators`.
- `ToneLookupCache`, `BrushCoverageCache` and their construction/work statistics.
- `RendererStatistics`: original six-field API plus CachedSources and SharedSourceHits.

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
session.Undo(); // Original encoded bytes are untouched.
```

### LightSpace.Editing

`EditorSession` makes each completed gesture one transaction. Previews update visible state without adding history; commit adds a step and cancel restores the opening state. Named versions, selective synchronization, frozen reference snapshots, albums and committed-revision recovery are included. Depends on Catalog and Storage; no UI.

```sh
dotnet add package LightSpace.Editing --prerelease
```

**Key types**
- `EditorSession`: Catalog, Active, Selection, Edit, BeginGesture/Preview/CommitGesture/CancelGesture, Undo/Redo, SaveVersion, SyncSelected, ApplySettings, CreateAlbum, Changed.
- `ReferencePhotoSnapshot.Capture(photo)`: independent render identity, shared immutable source and captured settings.
- `RecoveryPersistence(IRecoveryStore)`: Capture, CommitAsync, RestoreAsync.
- `RecoveryCoordinator`: FlushAsync, Status and StatusChanged; overlapping flush calls share one writer.

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
session.SyncSelected(); // Includes crop, geometry, optics and masks; retains target metadata.
session.Undo();

// Or transfer only selected groups in one transaction.
session.SyncSelected(EditSettingsGroup.Light | EditSettingsGroup.ColorGrading);

var persistence = new RecoveryPersistence(new FileRecoveryStore("recovery"));
using var recovery = RecoveryCoordinator.Incremental(session, persistence, initiallySaved: false);
await recovery.FlushAsync();
```

### LightSpace.Controls

Original dark Uno photography controls include active/reference viewports, selective settings, adjustment sliders, curves, grading/mixer/range/mask editors, optics/geometry, interactive histogram, panel-resize grips, thumbnails and chrome. Editors expose preview/commit/cancel events without owning the host's transaction policy. Brush settings affect new strokes. Depends on Editing and Rendering.Skia; requires the Uno Skia renderer.

```sh
dotnet add package LightSpace.Controls --prerelease
```

**Key types**
- `PhotoViewport(EditorSession, PhotoRenderer)`: SetTool, Fit, Compare, Before, Clipping, MaskOverlay, SetActiveMask, BrushSettings, Navigation and SetNavigation.
- `ReferencePhotoView(PhotoRenderer)`: Photo, Navigation, SetNavigation, NavigationChanged, Fit and RenderFailed. Does not own the renderer.
- `SettingsTransferEditor`: Groups and SelectionChanged; no document or clipboard ownership.
- `AdjustmentSlider`: ValueChanged, ValueCommitted, GestureCanceled.
- `PointCurveEditor`, `ColorGradingEditor`, `ColorMixerEditor`, `ColorRangeEditor`, `MaskSettingsEditor`, `GeometryEditor`, `OpticsEditor`: Value and preview/commit/cancel contracts.
- `BrushSettingsEditor`, `HistogramView`, `PanelResizeGrip`, `PhotoCard`, `PhotoThumbnail`, `ThumbnailCache`, `PanelSection`, `LightButton`, `Theme`.

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

// Keep values synchronized after undo, cancellation and external edits.
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
Grid.SetColumn(inspector, 1);
root.Children.Add(viewport); root.Children.Add(new ScrollViewer { Content = inspector });
Grid.SetColumn(root.Children[1], 1);
window.Content = root;
window.Closed += (_, _) => { viewport.Dispose(); renderer.Dispose(); };
```

See [reference embedding and navigation](docs/REFERENCE-AND-SYNC.md#reusable-model-and-ui-apis) for paired-view wiring and release of session-local render identities.

### LightSpace.Workbench

The complete application workspace as one control: library/grid, filmstrip, histogram, development/optics/geometry inspectors, frozen reference comparison, selective settings, tools, presets, versions, dialogs, shortcuts, resizable panels and recovery status. Depends on Controls and Storage; requires Uno. `ISidecarStorage` remains optional.

```sh
dotnet add package LightSpace.Workbench --prerelease
```

**Key types**
- `StudioView(EditorSession, IWorkspaceStorage, recoveryLoaded, RecoveryPersistence?)`: Session, Viewport, Recovery, SetGrid, ChooseTool, SetStatus, SaveRecoveryAsync.
- UnsavedChangesChanged and opt-in DiagnosticsChanged events.
- ProtectExistingRecovery preserves unreadable prior data until explicit replacement.

```csharp
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Storage;
using LightSpace.Workbench;

IWorkspaceStorage storage = new MyWorkspaceStorage(); // Your picker/export/recovery implementation.
var persistence = storage is IRecoveryStore store ? new RecoveryPersistence(store) : null;
var catalog = persistence is null ? null : await persistence.RestoreAsync();
var studio = new StudioView(new EditorSession(catalog ?? SamplePhotos.CreateCatalog()),
    storage, recoveryLoaded: catalog is not null, persistence: persistence);
window.Content = studio;
window.Closed += (_, _) => studio.Dispose();
```

A production host should catch restore failures and protect unreadable existing recovery rather than automatically replacing it; the supplied application host implements that behavior.

## Stack and rendering

Pinned versions are **.NET SDK 10.0.401**, **Uno SDK 6.7.30** and matched **SkiaSharp 3.119.4** managed/native assets. Upgrade the graphics family together and retain native ABI/lifetime validation.

The viewport uses `Uno.WinUI.Graphics2DSK.SKCanvasElement`. SkSL effects participate in Uno's existing Skia composition path. Hardware execution requires a GPU-backed host canvas. Software hosts remain supported; this is **not a separate WebGPU compute backend** or a fully GPU-based import/export pipeline.

Decode, first-use hashing, manifest serialization, brush texture publication, histogram sampling and image encoding retain synchronous CPU/native work. Viewport previews target 2560 pixels, thumbnails 384, brush coverage 1024 even for export, and image output 8192 pixels on the long edge. Cache budgets cover retained buffers, not every transient/GPU allocation. Channel alignment can evaluate the development child three times, so cost depends on enabled effects. Reference/active views share matching source decoding, but two visible panes still add draw and shading work. [Architecture](docs/ARCHITECTURE.md) · [Performance scopes](docs/PERFORMANCE.md) · [Shared-source ownership](docs/REFERENCE-AND-SYNC.md#shared-source-cache-and-gpu-work)

## Build and run

Install the SDK in `global.json`, Python 3 and your operating system's Uno desktop prerequisites.

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

Open `http://127.0.0.1:4173/LightSpace/`. Serve the actual Uno/.NET WebAssembly app over HTTP(S), not as a local HTML file. The build verifies the OFL font and optionally downloads demonstration photos. `LIGHTSPACE_NO_DEMO_PHOTOS=1` selects original generated landscapes.

## Tests and workflows

Build runs the engine suite, publishes WebAssembly, drives actual Uno controls, verifies exports/recovery and packages all libraries. It retains screenshots, traces on failure, source snapshots, structured results and scoped performance counters. Desktop compiles Windows/Linux/macOS. Pages consumes a successful trusted main build, verifies commit provenance and repeats browser tests against the public application.

The crash regression drives all 17 main development sliders through extremes and neutral crossings, then verifies undo/redo, JPEG decoding, all final catalog values and recovery reload. Photography tests cover optics, geometry, source picking, histogram/clipping, layout, export and cache reuse. Reference tests check frozen image pixels, independent shader state, shared-source lifetime, selective exported values, batch undo and linked framing. No editing features or fatal-error reporting are disabled for testing.

```bash
npm ci --ignore-scripts
npx playwright install --with-deps chromium
mkdir -p artifacts/fixtures
cp artifacts/engine/*.png artifacts/fixtures/
npm run test:browser
npx playwright test slider-stability.spec.mjs
npx playwright test reference-sync.spec.mjs reference-layout.spec.mjs
```

Release retains six-RID self-contained single-file desktop packaging, browser archives, symbol packages and checksums. Tags create GitHub releases and publish NuGet packages using OIDC Trusted Publishing from the protected `nuget` environment. **Manual runs are dry runs:** they build/upload artifacts without creating releases or publishing packages. [Release contract](docs/RELEASES.md)

CI uses Chromium/SwiftShader, not physical GPUs or certified pen hardware. CPU microbenchmarks, memory capacity and work counters are scoped evidence, not universal speed or leak-free claims. Signing/notarization, installers and automatic updates remain unconfigured.

## Compatibility and data safety

Catalog schema **5** preserves optics/geometry alongside masks and curves. Schemas 1–4 migrate with neutral missing fields; native XMP version 5 accepts 3–4. Recovery manifest format 1 and IndexedDB 2 are unchanged. The 0.6 reference/settings and 0.7 Survey increments add no persisted schema fields. Older builds reject unsupported processing schemas rather than silently discarding corrections. Keep pre-upgrade portable backups for older-version interoperability.

Safety ceilings are 64 MiB per source, 100 megapixels decoded and 256 MiB of encoded originals per catalog. Output is 8-bit sRGB, capped at 8192 pixels, without embedded source EXIF/IPTC. Recovery is local/unencrypted and does not merge tabs or automatically remove orphan sources. Keep original source files and portable backups.

RAW/DNG/HEIF/TIFF, AI tools, calibrated camera/lens profiles, automatic Upright, HDR/panorama, native-resolution tiling, durable indexed catalogs, virtual copies, printing/proofing and cloud workflows remain unimplemented. The interface is Lightroom-inspired, not full pixel-identical parity. [Complete feature ledger](docs/FEATURE-COVERAGE.md)

## Documentation and license

[Guide](docs/GETTING-STARTED.md) · [Survey](docs/SURVEY.md) · [Reference/settings](docs/REFERENCE-AND-SYNC.md) · [Optics/geometry](docs/OPTICS-GEOMETRY.md) · [Advanced editing/XMP](docs/ADVANCED-EDITING.md) · [Architecture](docs/ARCHITECTURE.md) · [Performance](docs/PERFORMANCE.md) · [Recovery](docs/RECOVERY.md) · [Slider hotfix](docs/WASM-SLIDER-FIX.md) · [Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Third-party notices](THIRD-PARTY-NOTICES.md)

Source is [MIT licensed](LICENSE). Dependencies, the OFL font and optional Unsplash photographs retain their own licenses; photos are not MIT-relicensed. Adobe and Lightroom are comparative workflow references and trademarks of their owners. No affiliation, endorsement or proprietary catalog compatibility is claimed.
