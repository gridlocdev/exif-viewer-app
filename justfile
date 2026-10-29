# EXIF Viewer dev recipes. Run `just` to list them.

default:
    @just --list

# Run the app, optionally opening a file: just run ~/Pictures/photo.jpg
run *file:
    dotnet run --project src/ExifViewer.App -- {{file}}

# Debug build of the whole solution
build:
    dotnet build

# Run the parser tests
test:
    dotnet test tests/ExifViewer.Core.Tests

# Package "EXIF Viewer.app" for this Mac's architecture into dist/
build-macos:
    scripts/package-mac.sh

# Package a universal (Apple Silicon + Intel) .app into dist/
build-macos-universal:
    scripts/package-mac.sh --arch universal

# Package a self-contained Linux x64 tarball into dist/ (works from macOS too)
build-linux:
    scripts/package-linux.sh

# Package a self-contained Windows x64 zip into dist/ (works from macOS or Linux)
build-windows:
    scripts/package-windows.sh

# Package and launch the .app
run-macos: build-macos
    open "dist/EXIF Viewer.app"

# Delete build output
clean:
    rm -rf build dist src/*/bin src/*/obj tests/*/bin tests/*/obj
