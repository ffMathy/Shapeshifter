#!/usr/bin/env bash
# Starts the existing Shapeshifter on a virtual X11 desktop, for the AIL end-to-end checks (ail.config.json run.legacy).
# Needs: the .NET SDK from global.json, xvfb, openbox, x11-utils, xdotool, and for the tray icon xfce4-panel.
#   sudo apt-get install -y xvfb openbox x11-utils xdotool xfce4-panel
# The desktop is $AIL_DISPLAY (default :97); settings, history and logs live in a fresh $AIL_STATE (default .ail/legacy).
# Several copies can run side by side, each with its own AIL_DISPLAY and AIL_STATE. AIL_SKIP_BUILD=1 skips the build.
set -euo pipefail
cd "$(dirname "$0")/../.."
export PATH="$HOME/.dotnet:$PATH" DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
state="${AIL_STATE:-.ail/legacy}"
rm -rf "$state" && mkdir -p "$state/home"

if [[ "${AIL_SKIP_BUILD:-}" != 1 ]]; then
  dotnet build src/Shapeshifter.Desktop -c Release -v quiet -nologo >"$state/build.log"
  dotnet build tests/Shapeshifter.EndToEnd.TargetApp -c Release -v quiet -nologo >>"$state/build.log"
fi

export DISPLAY="${AIL_DISPLAY:-:97}"
pids=()
cleanup() {
  for pid in "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
  for file in "$state/tray/panel.pid" "$state/tray/dbus.pid"; do [[ -f "$file" ]] && kill "$(cat "$file")" 2>/dev/null || true; done
}
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

# A system tray (a session D-Bus and a panel), since the tray menu is the way into the settings window on Linux.
if command -v xfce4-panel >/dev/null; then
  eval "$(checks/e2e/app/tray-host.sh "$state/tray")"
else
  echo "xfce4-panel is not installed: no tray icon, so the checks of the tray and the settings will fail" >&2
fi
# Links and folders the app opens are written to .ail/browser/<display>.txt instead of opening a browser.
export PATH="$PWD/checks/e2e/app/bin:$PATH"

SHAPESHIFTER_HOME="$(cd "$state" && pwd)/home" SHAPESHIFTER_NO_UPDATE=1 SHAPESHIFTER_NO_INSTALL=1 SHAPESHIFTER_LOOK=linux \
  dotnet src/Shapeshifter.Desktop/bin/Release/net11.0/Shapeshifter.dll --no-install >"$state/shapeshifter.log" 2>&1 &
pids+=($!)
wait "${pids[-1]}"
