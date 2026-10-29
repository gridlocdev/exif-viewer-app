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
- No preview for HEIC/video: decoding HEVC would need a codec dependency.
- UI is code-behind with compiled bindings; no MVVM toolkit.
- `just run [file]`, `just test`, `just publish <rid>`.
