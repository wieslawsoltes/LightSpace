# Survey culling and bounded rendering

Survey compares multiple photographs for selection, rating and flagging without changing their processing. It complements the editable single-photo and frozen-reference views. This is an independently implemented workflow, not a claim of pixel-identical Lightroom UI or processing.

## Open a survey

Choose **Survey view** in the left rail or press **N**. With two or more selected photographs, those become the candidates. Otherwise Survey uses the current filtered photo sequence and reports that choice. It retains candidate order and shows up to **twelve photos per page**; the previous/next controls and Page Up/Page Down visit the remaining candidates rather than silently dropping them. The active photo determines the opening page.

The edit inspector and single-photo toolbar are hidden while Survey is open. Photos use aspect-aware, centered rows and retain their entire crop; there is no automatic fill/cropping. The filmstrip, library filters, focus mode and filmstrip toggle remain available. Choosing an editing tool or Done returns to Detail. Opening Survey closes a previously pinned reference.

Each photo has an activation border, filename, rating, Pick, Reject and Exclude controls. Click a photo to activate it; use **0–5** for an exact rating and **P/X/U** to pick, reject or clear its flag. Clicking the rating cycles through zero to five. A rating or flag change targets **only that candidate**, even if the catalog's multi-selection contains other photos. It is an ordinary undoable edit and is saved through existing recovery.

Arrow keys navigate candidates across page boundaries. **Enter** or a double-click opens the active photo in Detail. **Z** opens source-pixel zoom geometry in Detail; this does not turn Survey previews into native-resolution tiles. **Y** opens single-photo before/after. Backslash or the Survey Original button toggles source color across the page, retaining crop/optical/projective alignment like the existing original view. Escape or Done closes Survey.

## Exclusion is not deletion

A tile's cross, or Delete/Backspace while Survey has focus, **excludes it from this survey only**. Neither the catalog record, original bytes, flag, rating nor processing settings are deleted or changed. The remaining candidates reflow. Hide rejected excludes currently rejected candidates; Restore excluded returns every candidate to its original position. Even excluding all candidates is recoverable with Restore.

Exclusions are session-only and are not document undo transactions. Ratings and flags do use document undo/redo. Closing/reopening Survey starts from the current selection/filter again; exclusions are not persisted across reload. The source-inclusive catalog still contains every source photo, including excluded and rejected candidates. Use the explicit tile controls to avoid confusing rejection, exclusion and file deletion.

The Current selection button rebuilds candidates from the current selection/filter. Control-click can update the filmstrip selection before using that button. Clicking a candidate in the filmstrip activates it without changing the survey group. Clicking a photo outside the group opens Detail. Replacing the catalog exits Survey and releases its references to the old catalog.

## GPU-capable display and preparation policy

`PhotoSurveyView` uses **one Uno SKCanvasElement** for all photo draws plus native Uno input/focus/accessibility chrome. Each prepared photo is sent directly to `PhotoRenderer.Draw`, preserving the composed development, optics and geometry pipeline. No CPU-rendered contact-sheet screenshot, JPEG intermediate or full-image readback is inserted for display. Whether this canvas is GPU-backed depends on Uno's host and the device; the code does not force a new WebGPU backend.

The survey renderer is deliberately separate from the detail renderer. It targets a **1024-pixel source-preview long edge**, twelve processing identities and **48 MiB of retained decoded source pixels**. The page size and budget agree, so a fully prepared twelve-photo page does not continually evict its first source while drawing its last. Immutable byte-array identity allows matching candidates to share a decoded image within this renderer. It does not share a decode pool with the separate detail/thumbnail renderer.

Before the first draw, a 16-millisecond dispatcher timer prepares **at most one photo per tick**. Preparation performs ordinary synchronous CPU/native decoding, uniform setup and brush preparation on the renderer owner thread. It is amortized work, **not asynchronous background decoding, a 16-millisecond execution-time guarantee, or GPU decoding**. A single complex decode can still block the UI; codec scratch memory is not bounded by the retained cache budget. Prepared photos appear progressively, and failed previews display a failure state rather than silently dropping the photo. Retry previews retries those failures.

Metadata changes and active-border updates retain cards, layout and pixel caches. Size/crop changes recompute layout; a processing/source change prepares that photo again. Moving pages releases processing identities no longer present, while retaining common candidates during reflow. Closing Survey stops preparation, clears its decoded/curve/brush caches and releases candidate controls and source references. Hidden detail-view histogram work is suspended while culling.

The 48 MiB number is **not total process or GPU memory**. Source bytes, CPU codec scratch allocations, native resources, curves, brush coverage caches, independent detail/thumbnail caches and GPU copies are outside it. Brush coverage keeps its existing 1024-pixel limit and separate cache budget. More visible photographs still require more draws and shading; bounded caching does not make that work free.

## Reusable APIs

`SurveyLayout`, `SurveyImage` and `SurveyTile` are UI-independent Core types. `SurveyLayout.Arrange` produces stable ordered rows, validates finite dimensions/positive aspects, and accepts only one bounded page. It fits a common image height with a monotone search and preserves image aspect in the caller's fit rectangle. The layout is deterministic, not an optimization claim about every possible photo arrangement.

`SurveySelection` in Editing owns candidate IDs, reversible exclusions, active navigation and pages without owning a catalog or touching recovery. It stages predicate-based exclusions before mutating its state. `EditorSession.ActivatePhoto` retains the multi-selection; `EditPhoto` targets one validated photo with ordinary atomic undo semantics.

```csharp
var survey = new SurveySelection();
survey.Open(selectedPhotoIds, session.Catalog.ActivePhoto);
survey.Changed += RefreshSurveyPage;

session.ActivatePhoto(survey.ActiveId); // Selection and document revision retained.
session.EditPhoto(survey.ActiveId, "Rate candidate", state => state with { Rating = 4 });
survey.Exclude(survey.ActiveId);       // View only; no catalog deletion.
survey.RestoreAll();
```

`PhotoSurveyView` and `SurveyCard` are reusable Uno controls. A host supplies a bounded list through `SetPhotos`, handles activation/rating/flag/exclusion events, and calls `SetActive` when showing/hiding the control. Dispose the view when its host closes. It owns its renderer; its models and all rendering operations remain owner-thread confined. Original arrays and state collections retain the existing read-only/copy-on-write contract.

## Validation and limits

Engine tests cover bounded/deterministic/non-overlapping layouts, mixed aspects, paging, duplicate IDs, empty restoration, failed predicates, missing targets, single-photo undo, unchanged multi-selection and shared source preparation. `survey-performance.json` measures 240 warmed **96×64 CPU/raster draws** across twelve independent sources, checking for zero additional decodes or shader builds. Its elapsed time is not hardware-GPU timing or full-size decode latency.

Browser tests drive real controls and keyboard input, export catalogs to check single-target metadata, restore exclusions, page a catalog larger than twelve photos, resize the workspace, return to Detail, reload recovery and record warmed work counters. The original all-slider fatal-crash regression and earlier masks/optics/reference/recovery suites remain enabled. CI uses Chromium/SwiftShader; native jobs certify compilation, not physical GPU behavior or complete accessibility coverage.

Survey is fit-only and session-local. It does not introduce virtual copies, stacks, synchronized multi-photo pixel zoom, automatic alignment, AI culling, RAW processing or native-resolution tiling. Catalog schema 5, native XMP 5, recovery manifest 1 and IndexedDB version 2 are unchanged. Keep portable backups and reload an old tab to load new code; do not clear site data.
