# Contributing

Keep the engine independent of Uno. Add new state in `LightSpace.Core`, normalization and catalog compatibility tests before exposing a new UI control. Treat array-backed snapshot members as read-only and use copy-on-write changes through `EditorSession`.

A change to the shader must include an actual pixel test, a neutral-input test where appropriate, and consideration of alpha, sRGB transfer encoding, crop, orientation and preview/export resolution. A new interactive tool must support pointer capture, cancel, one-gesture undo, and browser coverage driven by actual input rather than mutation-only testing hooks.

Run the engine console suite, publish the browser app, run Playwright, and build `net10.0-desktop` with `LightSpaceDesktopOnly=true`. Inspect the resulting screenshots and export artifacts. Do not treat a successful compiler run as validation of GPU acceleration, visual fidelity or native accessibility.

Keep managed/native Skia packages aligned through the root version property. Review dependency licenses, transitive native codec notices and asset redistribution terms. Do not introduce Adobe-owned code, profiles, fonts or artwork. Document incomplete behavior in the feature ledger rather than adding nonfunctional controls or claiming parity.

Pull requests should explain the user-visible change, affected reusable libraries, tests executed, screenshots for UI changes, and remaining platform limitations. Formatting follows `.editorconfig`; keep new types in clearly named files.
