#!/usr/bin/env bash
# Regenerates the screenshots in assets/screenshots by rendering the real picker window with sample content.
# Run it after changing how the picker looks. Rendering is headless, so no display is needed (CI runs this on pull
# requests and commits the result).
#
#   scripts/generate-screenshots.sh [output-folder] [--look windows,macos,linux] [--native-fonts]
#
# Each look should be rendered on its own operating system, where the font the app uses is installed (Segoe UI on
# Windows, SF Pro on macOS, Adwaita Sans on Linux: scripts/install-adwaita-fonts.sh); elsewhere it falls back to Inter
# with a warning. CI renders each look on its own runner.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet run --project tools/Shapeshifter.ScreenshotGenerator -c Release -- "${1:-assets/screenshots}" "${@:2}"
