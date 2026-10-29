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

# Self-contained single-file build for one runtime (osx-arm64, osx-x64, win-x64, linux-x64) into dist/<rid>/
publish rid:
    dotnet publish src/ExifViewer.App -c Release -r {{rid}} --self-contained -p:PublishSingleFile=true -o dist/{{rid}}
