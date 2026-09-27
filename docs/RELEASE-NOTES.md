# LightSpace 0.1.1-alpha.1

Revision-safe local recovery, explicit save/retry status, browser unsaved-change warnings, protected unreadable recovery, cancelable slider gestures, and source/state-aware rendering and thumbnail caches.

The recovery coordinator captures committed catalog snapshots rather than live previews and acknowledges only completed storage writes. Controlled asynchronous tests cover write races and failures. Real browser regressions hold a slider gesture across an earlier edit's autosave deadline, verify the actual IndexedDB payload, cancel with Escape, and protect unsupported recovery schemas until explicit replacement.

The shared Uno browser/desktop workspace and its eight reusable libraries remain independent of Adobe. This is an early functional implementation, not complete Lightroom parity. RAW, AI processing, calibrated lens correction, HDR/panorama merging, cloud services and signed native distribution are not included. See CHANGELOG.md, docs/RECOVERY.md and docs/FEATURE-COVERAGE.md.
