# Security and data handling

LightSpace is an early-stage local photo application. Do not use it as the sole copy of irreplaceable photographs. Save portable catalog backups and retain your original source files.

## Trust boundary

Image imports are untrusted codec input. Each source is capped at 64 MiB and 100 megapixels; catalog sources are capped at 256 MiB. Limits reduce accidental allocation but do not make native decoding a sandbox. Keep the complete Uno/Skia native and managed dependency family updated after compatibility testing. Malformed inputs should be added to the regression suite with redistribution permission.

Catalog files are data, not code. The application rejects unknown schemas, oversized sources, invalid dimensions and duplicate IDs, and prunes orphan album references. The source-generated serializer is used in WebAssembly. Imported captions and filenames are assigned as text rather than HTML. Export names are reduced to safe filename characters.

## Privacy and recovery

There is no LightSpace account, telemetry endpoint, photo upload, remote processing or embedded cloud client. Static hosting receives ordinary requests for application assets. The browser stores recovery in IndexedDB on the current origin. That data is not encrypted and can be accessed by other scripts with the same origin. Native recovery files are also unencrypted. Private browsing, storage cleanup, quota exhaustion and origin changes can remove access to recovery.

Only edits confirmed as saved in the footer are durable recovery writes. Multiple tabs are not coordinated; the last committed catalog write wins. Use one active editor tab per profile. Opening another catalog replaces the current in-memory workspace; export a backup first.

Read-only browser diagnostics are enabled by the `diagnostics` query parameter for testing. They contain UI geometry, filenames and current adjustment counters, not original image bytes. Do not expose those diagnostics to untrusted scripts.

## Reporting

Report a security issue privately through GitHub's repository security reporting facility when available. Do not post sensitive photographs, credential material, or a working exploit in a public issue. Ordinary reproducible rendering defects can be reported with a minimal non-sensitive image and exact platform/runtime versions.
