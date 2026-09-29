# Release packaging and publication

The existing user-maintained release workflow is preserved unchanged by the 0.5 photography increment. It builds the browser app, eight libraries with symbols and six self-contained native single-file executables: Windows/Linux/macOS on x64 and arm64. Native dependencies extract at startup. Single-file packaging is not code signing, notarization or an installer.

## Version tags and manual dry runs

A `vMAJOR.MINOR.PATCH[-prerelease]` tag resolves the release version and triggers publication. The manual dispatch accepts an explicit version input and is a **dry run**: all artifacts and checksums are built/uploaded, but no GitHub release or NuGet publication occurs. Select the intended version explicitly rather than relying on the older default input. A source-version bump alone does not create a release tag.

The version job validates semantic-version syntax. Libraries run engine checks, pack packages/symbols and publish the browser ZIP. Native jobs cross-publish each RID from Linux, rename the executable to LightSpace, and verify that its output consists of one file before archiving. Cross-publication is not runtime certification on those target operating systems.

## Native artifact names

Windows uses `LightSpace-<version>-win-x64.zip` and `LightSpace-<version>-win-arm64.zip`. Linux and macOS use the same versioned naming convention with their RID and `.tar.gz`, preserving executable permissions. Extract and run LightSpace (LightSpace.exe on Windows). Verify downloaded files against the published SHA256SUMS.txt and their expected source.

The collection job uploads a combined lightspace-release artifact for both tags and manual dry runs. Only tag runs create/update the GitHub release. Existing tagged assets may be replaced by a rerun; prerelease version names remain marked as prereleases.

## NuGet Trusted Publishing

The NuGet job runs only for version tags, after the release job, using the protected `nuget` environment. It obtains a short-lived API key through `NuGet/login` and GitHub OIDC rather than a stored long-lived publishing key. The configured NuGet account must own the packages and have a matching Trusted Publisher policy.

`NUGET_USER` is read from the repository/environment variable or secret with that name; an absent value fails validation. The original workflow does not assume a default account. The publishing job alone receives the required OIDC permission.

Packages and `.snupkg` symbols are uploaded as the dedicated nuget-packages artifact. `dotnet nuget push` handles the package publication and corresponding symbol package, with `--skip-duplicate` for immutable existing versions. Package metadata and SourceLink identify the build commit. Build artifact creation is not proof that the NuGet publishing job was run or that a particular source version is available online.

## Local single-file publication

```bash
dotnet publish src/LightSpace.App/LightSpace.App.csproj \
  -c Release -f net10.0-desktop -p:LightSpaceDesktopOnly=true \
  -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:IncludeAllContentForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=none -p:DebugSymbols=false \
  -o artifacts/native/linux-x64
```

Use the appropriate RID and test native dependencies, file pickers and GPU drivers on the target. Signing/notarization, installers and automatic updates require separate configuration.

## Pages versus releases

The ordinary Build workflow validates engine and actual-browser behavior, packages libraries and retains source/test artifacts. Pages consumes a successful trusted main Build artifact, checks its commit, deploys and repeats public-site tests. This pipeline is independent of release-tag publication and does not itself publish NuGet packages.
