#!/usr/bin/env bash
# Gives an X11 desktop a system tray, as a Linux desktop has: a session D-Bus and a panel at the bottom of the screen
# whose notification area shows StatusNotifierItem icons (xfce4-panel's "systray" plugin) and their menus.
# Shapeshifter (Avalonia) shows its tray icon only over D-Bus, so start this BEFORE Shapeshifter and give Shapeshifter
# the bus address it prints:
#   eval "$(DISPLAY=:105 checks/e2e/app/tray-host.sh .ail/app-tray)"   # exports DBUS_SESSION_BUS_ADDRESS
# Needs: dbus-daemon, xfce4-panel (sudo apt-get install -y xfce4-panel). Everything it writes stays under the folder;
# the bus and the panel run until killed (their process ids are in dbus.pid and panel.pid there).
set -euo pipefail
dir="$(mkdir -p "${1:?usage: tray-host.sh <state folder>}" && cd "$1" && pwd)"
: "${DISPLAY:?DISPLAY must be set}"
mkdir -p "$dir/config/xfce4/xfconf/xfce-perchannel-xml" "$dir/runtime" && chmod 700 "$dir/runtime"
cat >"$dir/config/xfce4/xfconf/xfce-perchannel-xml/xfce4-panel.xml" <<'XML'
<?xml version="1.0" encoding="UTF-8"?>
<channel name="xfce4-panel" version="1.0">
  <property name="configver" type="int" value="2"/>
  <property name="panels" type="array">
    <value type="int" value="1"/>
    <property name="panel-1" type="empty">
      <property name="position" type="string" value="p=8;x=0;y=0"/>
      <property name="length" type="uint" value="100"/>
      <property name="position-locked" type="bool" value="true"/>
      <property name="size" type="uint" value="32"/>
      <property name="icon-size" type="uint" value="24"/>
      <property name="plugin-ids" type="array">
        <value type="int" value="1"/>
        <value type="int" value="2"/>
      </property>
    </property>
  </property>
  <property name="plugins" type="empty">
    <property name="plugin-1" type="string" value="separator">
      <property name="expand" type="bool" value="true"/>
      <property name="style" type="uint" value="0"/>
    </property>
    <property name="plugin-2" type="string" value="systray">
      <property name="square-icons" type="bool" value="true"/>
    </property>
  </property>
</channel>
XML
address="unix:path=$dir/runtime/bus"
if ! DBUS_SESSION_BUS_ADDRESS="$address" gdbus call --session -d org.freedesktop.DBus -o /org/freedesktop/DBus -m org.freedesktop.DBus.GetId >/dev/null 2>&1; then
  rm -f "$dir/runtime/bus"
  # Services the bus starts (xfconfd, which keeps the panel's settings) inherit this folder as their home.
  HOME="$dir" XDG_CONFIG_HOME="$dir/config" XDG_DATA_HOME="$dir/data" XDG_CACHE_HOME="$dir/cache" XDG_RUNTIME_DIR="$dir/runtime" \
    dbus-daemon --session --address="$address" --fork --print-pid >"$dir/dbus.pid"
fi
export DBUS_SESSION_BUS_ADDRESS="$address"
if ! { [[ -f "$dir/panel.pid" ]] && kill -0 "$(cat "$dir/panel.pid")" 2>/dev/null; }; then
  HOME="$dir" XDG_CONFIG_HOME="$dir/config" XDG_DATA_HOME="$dir/data" XDG_CACHE_HOME="$dir/cache" XDG_RUNTIME_DIR="$dir/runtime" \
    nohup xfce4-panel --disable-wm-check >"$dir/panel.log" 2>&1 &
  echo $! >"$dir/panel.pid"
  sleep 2
fi
echo "export DBUS_SESSION_BUS_ADDRESS='$address'"
