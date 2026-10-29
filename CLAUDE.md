# exif-viewer

Cross-platform EXIF viewer: C#/.NET 10 + Avalonia 12. See README.md.

- Keep third-party dependencies minimal (the user's explicit wish). ExifViewer.Core has no package
  references and must stay that way; the app takes only Avalonia. Ask before adding any package.
- Package versions are central in `Directory.Packages.props`; `.csproj` references are version-less.
- Warnings are errors (`Directory.Build.props`).
- Parsers never throw on bad input: out-of-range reads throw `MalformedDataException` from
  `EndianReader`, and readers catch it and add a warning to the report while keeping what they read.
- Tag value formatting is culture-invariant (`TagFormatter`), so tests are stable.
- Test fixtures are built in code (`TiffBuilder`, `Containers` in `Fixtures.cs`; `Iso`/`IsoFixtures` for HEIC and MOV); don't commit sample photos.
- Movies (ISO media that isn't HEIF) load via `IsoMedia.LoadWithoutMediaData`, which drops `mdat` payloads; keep readers independent of absolute media offsets.
- Preview fallback on macOS is Quick Look (`SystemPreview`, shells out to `/usr/bin/qlmanage`; thumbnails arrive already rotated). Other platforms show "No preview" for HEIC/video; decoding HEVC would need a codec dependency.
- UI is code-behind with compiled bindings; no MVVM toolkit.
- `just run [file]`, `just test`, `just build-macos|build-macos-universal|build-windows|build-linux` (scripts in `scripts/`, output in `dist/`).
- CI builds and tests on all three OSes; pushing a `v*` tag publishes a GitHub release. The app version lives in `ExifViewer.App.csproj`.
- User docs live in `docs/` (wiki-style: `Home.md`, `_Sidebar.md`, flat hyphenated pages, relative `.md` links). Update the page describing a behaviour in the same commit that changes it.
