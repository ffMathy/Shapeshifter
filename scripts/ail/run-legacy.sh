#!/usr/bin/env bash
# Starts the existing Shapeshifter on a virtual X11 desktop, for the AIL end-to-end checks (ail.config.json run.legacy).
# Needs: the .NET SDK from global.json, xvfb, openbox, x11-utils, xdotool.
#   sudo apt-get install -y xvfb openbox x11-utils xdotool
# The desktop is $AIL_DISPLAY (default :97); settings and history live in a fresh folder under .ail/legacy.
set -euo pipefail
cd "$(dirname "$0")/../.."
export PATH="$HOME/.dotnet:$PATH" DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
state=.ail/legacy
rm -rf "$state" && mkdir -p "$state/home"

dotnet build src/Shapeshifter.Desktop -c Release -v quiet -nologo >"$state/build.log"
dotnet build tests/Shapeshifter.EndToEnd.TargetApp -c Release -v quiet -nologo >>"$state/build.log"

export DISPLAY="${AIL_DISPLAY:-:97}"
pids=()
cleanup() { for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done; }
trap cleanup EXIT INT TERM

if ! xdpyinfo >/dev/null 2>&1; then
  Xvfb "$DISPLAY" -screen 0 1280x800x24 -nolisten tcp >"$state/xvfb.log" 2>&1 &
  pids+=($!)
  for _ in $(seq 50); do xdpyinfo >/dev/null 2>&1 && break; sleep 0.1; done
fi
# Keyboard focus only moves between windows with a window manager.
if ! xprop -root _NET_SUPPORTING_WM_CHECK 2>/dev/null | grep -q "window id #"; then
  openbox >"$state/openbox.log" 2>&1 &
  pids+=($!)
  sleep 1
fi

SHAPESHIFTER_HOME="$PWD/$state/home" SHAPESHIFTER_NO_UPDATE=1 SHAPESHIFTER_NO_INSTALL=1 SHAPESHIFTER_LOOK=linux \
  dotnet src/Shapeshifter.Desktop/bin/Release/net11.0/Shapeshifter.dll --no-install >"$state/shapeshifter.log" 2>&1 &
pids+=($!)
wait "${pids[-1]}"
