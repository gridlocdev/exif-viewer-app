#!/usr/bin/env bash
# Renders assets/icon.svg into the icon files the app and packages use. Needs Inkscape (macOS: iconutil too).
#
#   scripts/make-icons.sh
#
# Output, committed so builds don't need Inkscape:
#   src/ExifViewer.App/Assets/icon.icns  macOS bundle icon (Apple grid, with margin)
#   src/ExifViewer.App/Assets/icon.ico   Windows .exe icon (tile only, 16-256 px)
#   src/ExifViewer.App/Assets/icon.png   window icon (tile only, 256 px)
set -euo pipefail

cd "$(dirname "$0")/.."

SRC="assets/icon.svg"
OUT="src/ExifViewer.App/Assets"
WORK="build/icons"

render() { inkscape "$1" --export-type=png --export-filename="$2" -w "$3" -h "$3" 2>/dev/null; }

mkdir -p "$OUT" "$WORK"
rm -rf "$WORK"/*

# The tile without Apple's margin: same drawing, viewBox cropped to the tile.
sed 's/viewBox="0 0 1024 1024"/viewBox="100 100 824 824"/' "$SRC" > "$WORK/tile.svg"

echo "==> icon.icns"
ICONSET="$WORK/icon.iconset"
mkdir -p "$ICONSET"
for size in 16 32 128 256 512; do
  render "$SRC" "$ICONSET/icon_${size}x${size}.png" "$size"
  render "$SRC" "$ICONSET/icon_${size}x${size}@2x.png" $((size * 2))
done
iconutil -c icns "$ICONSET" -o "$OUT/icon.icns"

echo "==> icon.ico, icon.png"
for size in 16 24 32 48 64 128 256; do
  render "$WORK/tile.svg" "$WORK/tile-$size.png" "$size"
done
cp "$WORK/tile-256.png" "$OUT/icon.png"
# An ICO is a directory of PNG images (supported since Windows Vista).
python3 - "$WORK" "$OUT/icon.ico" <<'PY'
import struct, sys
work, out = sys.argv[1], sys.argv[2]
sizes = [16, 24, 32, 48, 64, 128, 256]
images = [open(f"{work}/tile-{s}.png", "rb").read() for s in sizes]
offset = 6 + 16 * len(sizes)
data = struct.pack("<HHH", 0, 1, len(sizes))
for size, png in zip(sizes, images):
    data += struct.pack("<BBBBHHII", size % 256, size % 256, 0, 0, 1, 32, len(png), offset)
    offset += len(png)
open(out, "wb").write(data + b"".join(images))
PY

ls -la "$OUT"
