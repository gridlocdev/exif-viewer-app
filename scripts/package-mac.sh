#!/usr/bin/env bash
# Builds "EXIF Viewer.app" (self-contained, no .NET install needed).
#
#   scripts/package-mac.sh [--arch arm64|x64|universal] [--out DIR]
#
# APP_VERSION overrides the <Version> from the .csproj (the release workflow sets it from the git tag).
#
# Signing: ad-hoc by default (fine for your own Mac). Set MAC_SIGN_IDENTITY to a
# "Developer ID Application: ..." identity to sign with the hardened runtime for distribution
# (notarise afterwards with `xcrun notarytool`).
set -euo pipefail

cd "$(dirname "$0")/.."

APP_NAME="EXIF Viewer"
EXE_NAME="ExifViewer"
BUNDLE_ID="dev.gridloc.exifviewer"
PROJECT="src/ExifViewer.App/ExifViewer.App.csproj"

case "$(uname -m)" in arm64) ARCH="arm64" ;; *) ARCH="x64" ;; esac
OUT="dist"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --arch) ARCH="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    -h|--help) sed -n '2,10p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done
[[ "$ARCH" =~ ^(arm64|x64|universal)$ ]] || { echo "--arch must be arm64, x64 or universal" >&2; exit 1; }

VERSION="${APP_VERSION:-$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT" | head -1)}"
VERSION="${VERSION:-0.1.0}"
BUILD_DIR="build/mac"

publish() {
  local rid="osx-$1"
  echo "==> Publishing $rid"
  dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
    -p:UseAppHost=true -p:Version="$VERSION" -p:DebugType=None \
    -o "$BUILD_DIR/publish-$1" --nologo -v quiet
  # Native debug symbols that NuGet packages ship (libSkiaSharp.pdb is ~80 MB).
  find "$BUILD_DIR/publish-$1" -name "*.pdb" -delete
}

# Merge two single-arch publish folders into one, lipo-ing every thin Mach-O binary.
merge_universal() {
  local a="$BUILD_DIR/publish-arm64" x="$BUILD_DIR/publish-x64" u="$BUILD_DIR/publish-universal"
  echo "==> Merging arm64 + x64 into a universal build"
  rm -rf "$u"
  mkdir -p "$u"
  (cd "$a" && find . -type f) | while read -r f; do
    mkdir -p "$u/$(dirname "$f")"
    if [[ -f "$x/$f" ]] && ! cmp -s "$a/$f" "$x/$f" && file "$a/$f" | grep -q "Mach-O" &&
       [[ "$(lipo -archs "$a/$f" | wc -w)" -eq 1 ]]; then
      lipo -create "$a/$f" "$x/$f" -output "$u/$f"
    else
      cp "$a/$f" "$u/$f"
    fi
  done
}

write_plist() {
  cat > "$1" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>${APP_NAME}</string>
  <key>CFBundleDisplayName</key><string>${APP_NAME}</string>
  <key>CFBundleIdentifier</key><string>${BUNDLE_ID}</string>
  <key>CFBundleExecutable</key><string>${EXE_NAME}</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>${VERSION}</string>
  <key>CFBundleVersion</key><string>${VERSION}</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.photography</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSHumanReadableCopyright</key><string>Shows the metadata inside photos and videos.</string>
</dict>
</plist>
PLIST
}

# ---------------------------------------------------------------------------- build

mkdir -p "$BUILD_DIR" "$OUT"
if [[ "$ARCH" == "universal" ]]; then
  publish arm64
  publish x64
  merge_universal
else
  publish "$ARCH"
fi
PUBLISH="$BUILD_DIR/publish-$ARCH"

APP="$OUT/$APP_NAME.app"
if [[ -e "$APP" ]]; then
  # Move the old bundle aside rather than deleting it outright.
  mv "$APP" "$BUILD_DIR/previous-$(date +%Y%m%d-%H%M%S).app"
fi

echo "==> Assembling $APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH/." "$APP/Contents/MacOS/"
write_plist "$APP/Contents/Info.plist"
printf 'APPL????' > "$APP/Contents/PkgInfo"
plutil -lint -s "$APP/Contents/Info.plist"

echo "==> Signing"
if [[ -n "${MAC_SIGN_IDENTITY:-}" ]]; then
  # .NET needs JIT + unsigned executable memory under the hardened runtime.
  ENTITLEMENTS="$BUILD_DIR/entitlements.plist"
  cat > "$ENTITLEMENTS" <<'ENT'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>com.apple.security.cs.allow-jit</key><true/>
  <key>com.apple.security.cs.allow-unsigned-executable-memory</key><true/>
  <key>com.apple.security.cs.disable-library-validation</key><true/>
</dict>
</plist>
ENT
  find "$APP/Contents/MacOS" -type f \( -name "*.dylib" -o -perm -u+x \) -print0 |
    xargs -0 codesign --force --timestamp --options runtime --entitlements "$ENTITLEMENTS" --sign "$MAC_SIGN_IDENTITY"
  codesign --force --timestamp --options runtime --entitlements "$ENTITLEMENTS" --sign "$MAC_SIGN_IDENTITY" "$APP"
else
  codesign --force --deep --sign - "$APP"
fi
codesign --verify --deep --strict "$APP"

echo
echo "Built: $APP  ($ARCH, v$VERSION, $(du -sh "$APP" | cut -f1))"
echo "Run it with:  open \"$APP\""
