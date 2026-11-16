#!/usr/bin/env bash
# Regenerates the screenshots in assets/screenshots by rendering the real picker window with sample content.
# Run it after changing how the picker looks. Rendering is headless, so no display is needed, and the output is
# the same on every machine (CI runs this on pull requests and commits the result).
#
#   scripts/generate-screenshots.sh [output-folder]
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet run --project tools/Shapeshifter.ScreenshotGenerator -c Release -- "${1:-assets/screenshots}"
