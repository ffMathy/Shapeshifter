#!/usr/bin/env bash
# Wraps a dotnet publish output folder into a signed .app and a signed installer package for the Mac App Store.
#
#   packaging/store/macos/make-pkg.sh <publish-dir> <version> <output-dir>
#
# <publish-dir> is the output of
#   dotnet publish -r osx-arm64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false
# so Contents/MacOS holds only Mach-O code: the single-file executable (with every managed assembly inside) and the native
# libraries next to it. Each is signed on its own; data files there would carry their signature in extended attributes,
# which installer packages can lose ("the app is damaged").
#
# Environment:
#   APPLE_TEAM_ID              the team ID (10 characters) from developer.apple.com > Membership
#   MAS_APP_IDENTITY           code signing identity: "Apple Distribution: …" (or "3rd Party Mac Developer Application: …")
#   MAS_INSTALLER_IDENTITY     installer identity: "3rd Party Mac Developer Installer: …" (shown as "Mac Installer Distribution")
#   MAS_PROVISIONING_PROFILE   path to the app's Mac App Store provisioning profile (.provisionprofile)
#   MAS_KEYCHAIN               optional keychain that holds both identities
#   MAS_EXTRA_RESOURCES        optional files (separated by spaces) to copy into Contents/Resources, such as notices
set -euo pipefail

publish_dir="${1:?publish dir}"
version="${2:?version, such as 7.1.3}"
out_dir="${3:?output dir}"
here="$(cd "$(dirname "$0")" && pwd)"
: "${APPLE_TEAM_ID:?}" "${MAS_APP_IDENTITY:?}" "${MAS_INSTALLER_IDENTITY:?}" "${MAS_PROVISIONING_PROFILE:?}"
keychain=()
if [[ -n "${MAS_KEYCHAIN:-}" ]]; then keychain=(--keychain "$MAS_KEYCHAIN"); fi

if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Version must be X.Y.Z, not $version (prereleases do not go to the App Store)" >&2
  exit 1
fi

plist() { /usr/libexec/PlistBuddy -c "Print :$1" "$here/Info.plist"; }
name="$(plist CFBundleExecutable)"
bundle_id="$(plist CFBundleIdentifier)"
work="$(mktemp -d)"
app="$work/$name.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$out_dir"

cp -R "$publish_dir"/. "$app/Contents/MacOS/"
find "$app/Contents/MacOS" -name '*.pdb' -delete
chmod +x "$app/Contents/MacOS/$name"
sed "s/__VERSION__/$version/g" "$here/Info.plist" > "$app/Contents/Info.plist"
plutil -lint "$app/Contents/Info.plist"
printf 'APPL????' > "$app/Contents/PkgInfo"
cp "$MAS_PROVISIONING_PROFILE" "$app/Contents/embedded.provisionprofile"
for resource in ${MAS_EXTRA_RESOURCES:-}; do
  cp "$resource" "$app/Contents/Resources/"
done

# The App Store needs every size up to 512 pt @2x (1024 px) in the .icns.
iconset="$work/AppIcon.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$here/AppIcon.png" --out "$iconset/icon_${size}x${size}.png" > /dev/null
  sips -z $((size * 2)) $((size * 2)) "$here/AppIcon.png" --out "$iconset/icon_${size}x${size}@2x.png" > /dev/null
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/AppIcon.icns"

entitlements="$work/Entitlements.plist"
sed -e "s/__TEAM_ID__/$APPLE_TEAM_ID/g" -e "s/__BUNDLE_ID__/$bundle_id/g" "$here/Entitlements.plist" > "$entitlements"
plutil -lint "$entitlements"

# Inside out, without --deep: the libraries first, then the bundle, which signs the main executable with the entitlements.
data_files=()
while IFS= read -r -d '' file; do
  if [[ "$file" == "$app/Contents/MacOS/$name" ]]; then continue; fi
  if file -b "$file" | grep -q 'Mach-O'; then
    codesign --force --sign "$MAS_APP_IDENTITY" ${keychain[@]+"${keychain[@]}"} --timestamp "$file"
  else
    data_files+=("${file#"$app/"}")
  fi
done < <(find "$app/Contents/MacOS" -type f -print0)
if [[ ${#data_files[@]} -gt 0 ]]; then
  echo "Contents/MacOS must hold only code, but it has: ${data_files[*]}" >&2
  echo "Publish with -p:PublishSingleFile=true so managed assemblies and data go inside the executable." >&2
  exit 1
fi
codesign --force --sign "$MAS_APP_IDENTITY" ${keychain[@]+"${keychain[@]}"} --timestamp --entitlements "$entitlements" "$app"
codesign --verify --strict --deep --verbose=2 "$app"
codesign --display --entitlements - "$app"

pkg="$out_dir/$name-$version.pkg"
productbuild --component "$app" /Applications --sign "$MAS_INSTALLER_IDENTITY" ${keychain[@]+"${keychain[@]}"} "$pkg"
pkgutil --check-signature "$pkg"
rm -rf "$work"
echo "Created $pkg"
