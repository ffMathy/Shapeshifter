#!/usr/bin/env bash
# Runs the real end-to-end tests on Linux. Starts a virtual X server (Xvfb) and a window manager (openbox)
# when no display is available, so it works on headless machines and CI.
#
#   sudo apt-get install -y xvfb openbox      # once
#   scripts/run-e2e-linux.sh [-c Release]
set -euo pipefail
cd "$(dirname "$0")/.."

pids=()
cleanup() { for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done; }
trap cleanup EXIT

if [[ -z "${DISPLAY:-}" ]] || ! xdpyinfo >/dev/null 2>&1; then
  export DISPLAY=:97
  Xvfb "$DISPLAY" -screen 0 1280x800x24 -nolisten tcp &
  pids+=($!)
  for _ in $(seq 50); do xdpyinfo >/dev/null 2>&1 && break; sleep 0.1; done
fi

# A window manager is required for keyboard focus to move between windows.
if ! xprop -root _NET_SUPPORTING_WM_CHECK 2>/dev/null | grep -q "window id #"; then
  openbox &
  pids+=($!)
  sleep 1
fi

SHAPESHIFTER_E2E=1 dotnet test --project tests/Shapeshifter.EndToEnd.Tests "$@"
