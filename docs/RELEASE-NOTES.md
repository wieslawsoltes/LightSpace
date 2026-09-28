# LightSpace 0.4.0-alpha.1

Completes the advanced RGB-curve, freehand-brush and XMP work in PR #9, then adds sampled color-range masks and content-addressed recovery in PR #10.

Color selection supports up to five averaged source-preview samples, click/Shift-click/Alt-click editing, tolerance, smoothness, local restrictions and viewport-only coverage. Existing mask operations, undo, native settings interchange and versions preserve these settings. The Oklab-based selection is original processing, not Adobe algorithm equivalence.

Recovery separates immutable SHA-256-addressed originals from committed edit manifests. Warm metadata changes avoid original-byte serialization, rehashing and rewrites. Restore verifies length and hash; missing/damaged recovery remains protected until explicit replacement. Browser publication aborts on missing references and both asynchronous and synchronous failures. IndexedDB requests strict durability as a hint, not a power-loss guarantee. An explicit retry can restage missing originals.

Catalog schema 4 migrates versions 1–3. Native XMP settings version 4 accepts version 3. IndexedDB upgrades to version 2; older builds requesting database version 1 cannot open that upgraded store. Portable catalog exports still embed original bytes. Preserve portable pre-upgrade backups for older-version interoperability.

All eight libraries and shared Uno hosts retain build, desktop, Pages and release workflows. Full RAW/AI, calibrated profiles, HDR/panorama, native-resolution tiling, print/proofing, cloud synchronization and signed distribution remain outside this release. See FEATURE-COVERAGE.md, RECOVERY.md and PERFORMANCE.md for the precise boundaries.
