#!/usr/bin/env bash
# Serves the website locally the way GitHub Pages will: copies the current screenshots and app icons into website/
# (both folders are git-ignored), as .github/workflows/website.yml does at build time, then serves the folder.
#
#   website/preview.sh [port]
#
# The settings and notification screenshots are rendered with the .NET SDK when it is installed; without it, those
# images are missing from the preview.
set -euo pipefail
cd "$(dirname "$0")/.."

mkdir -p website/screenshots website/app-icons
cp assets/screenshots/*.PNG website/screenshots/
for look in Windows MacOS Linux; do
  cp "src/Shapeshifter.Desktop/Assets/$look/Application.png" "website/app-icons/${look,,}.png"
done

if command -v dotnet > /dev/null; then
  surfaces=$(mktemp -d)
  trap 'rm -rf "$surfaces"' EXIT
  dotnet run --project tools/Shapeshifter.ScreenshotGenerator -c Release -- "$surfaces" --surfaces > /dev/null
  cp "$surfaces"/settings-*.PNG "$surfaces"/toast-*.PNG website/screenshots/
else
  echo "dotnet not found: the settings and notification screenshots are left out." >&2
fi

port="${1:-8000}"
echo "Serving http://localhost:$port"
cd website
exec python3 -m http.server "$port"
