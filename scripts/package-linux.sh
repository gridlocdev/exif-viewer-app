#!/usr/bin/env bash
# Builds a self-contained Linux x64 tarball (no .NET install needed).
#
#   scripts/package-linux.sh [--out DIR]
#
# APP_VERSION overrides the <Version> from the .csproj (the release workflow sets it from the git tag).
#
# Output: DIR/exif-viewer-<version>-linux-x64.tar.gz, which unpacks to exif-viewer/.
# Can be run from macOS or Linux.
set -euo pipefail

cd "$(dirname "$0")/.."

PKG_NAME="exif-viewer"
EXE_NAME="ExifViewer"
PROJECT="src/ExifViewer.App/ExifViewer.App.csproj"
RID="linux-x64"
OUT="dist"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --out) OUT="$2"; shift 2 ;;
    -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

VERSION="${APP_VERSION:-$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT" | head -1)}"
VERSION="${VERSION:-0.1.0}"
BUILD_DIR="build/linux"
STAGE="$BUILD_DIR/$PKG_NAME"
TARBALL="$OUT/$PKG_NAME-$VERSION-$RID.tar.gz"

mkdir -p "$BUILD_DIR" "$OUT"
if [[ -e "$STAGE" ]]; then
  mv "$STAGE" "$BUILD_DIR/previous-$(date +%Y%m%d-%H%M%S)"
fi

echo "==> Publishing $RID"
dotnet publish "$PROJECT" -c Release -r "$RID" --self-contained true \
  -p:UseAppHost=true -p:Version="$VERSION" -p:DebugType=None \
  -o "$STAGE" --nologo -v quiet
chmod +x "$STAGE/$EXE_NAME"
# Native debug symbols that NuGet packages ship (libSkiaSharp.pdb is ~80 MB).
find "$STAGE" -name "*.pdb" -delete

echo "==> Creating $TARBALL"
# COPYFILE_DISABLE keeps macOS tar from adding ._ metadata files.
COPYFILE_DISABLE=1 tar -czf "$TARBALL" -C "$BUILD_DIR" "$PKG_NAME"

echo
echo "Built: $TARBALL  (v$VERSION, $(du -sh "$TARBALL" | cut -f1))"
echo "Run it with:  tar -xzf $(basename "$TARBALL") && ./$PKG_NAME/$EXE_NAME"
