# Installing

EXIF Viewer comes as a ready-to-run download for each platform. Every download includes
everything it needs, so you don't have to install .NET or anything else first.

Downloads are on the project's GitHub **Releases** page. Pick the file for your system:

| System | File | How to start it |
|---|---|---|
| macOS 12 or later (Apple Silicon and Intel) | `exif-viewer-<version>-macos-universal.zip` | Unzip, then open **EXIF Viewer.app** |
| Windows 10/11 (x64) | `exif-viewer-<version>-win-x64.zip` | Unzip, then run `exif-viewer\ExifViewer.exe` |
| Linux (x64) | `exif-viewer-<version>-linux-x64.tar.gz` | `tar -xzf` it, then run `./exif-viewer/ExifViewer` |

## First launch warnings

The builds aren't signed with a paid developer certificate yet, so your system may warn you the
first time:

- **macOS** won't open the app the first time and says Apple can't check it for malicious
  software. You only need to get past this once:
  - **macOS 15 (Sequoia) or later:** after the warning, open **System Settings → Privacy &
    Security**, scroll down to the message about EXIF Viewer, click **Open Anyway** and confirm
    with your password or Touch ID.
  - **macOS 12 to 14:** right-click (or Control-click) the app, choose **Open**, then **Open**
    again.
  - **Any version**, in Terminal: `xattr -dr com.apple.quarantine "EXIF Viewer.app"`, then open
    the app normally.
- **Windows** SmartScreen may say "Windows protected your PC". Choose **More info**, then
  **Run anyway**.

## Building it yourself

If you'd rather build from source, you need the .NET 10 SDK and
[just](https://github.com/casey/just). From the project folder:

```bash
just run            # build and open the app
just run-macos      # package EXIF Viewer.app into dist/ and open it
just build-windows  # make the Windows zip in dist/
just build-linux    # make the Linux tarball in dist/
```

More detail is in the [Developer guide](Developer-Guide.md).

---
Next: [Using the app](Using-the-App.md)
