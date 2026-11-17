#!/usr/bin/env bash
# Installs Adwaita Sans and Adwaita Mono (GNOME's interface fonts, SIL Open Font License) for the current user, so the
# Linux look's screenshots are rendered in the font GNOME shows it in. Used by CI before rendering the Linux look.
#
#   scripts/install-adwaita-fonts.sh [version]
set -euo pipefail
version="${1:-51.0}"
major="${version%%.*}"
target="${XDG_DATA_HOME:-$HOME/.local/share}/fonts/adwaita"
if fc-list | grep -q "Adwaita Sans"; then
  echo "Adwaita Sans is already installed."
  exit 0
fi
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
curl -sSfL "https://download.gnome.org/sources/adwaita-fonts/$major/adwaita-fonts-$version.tar.xz" -o "$work/fonts.tar.xz"
tar -xJf "$work/fonts.tar.xz" -C "$work"
mkdir -p "$target"
find "$work" \( -name '*.ttf' -o -name '*.otf' \) -exec cp {} "$target/" \;
fc-cache -f "$target" > /dev/null
fc-list | grep "Adwaita"
