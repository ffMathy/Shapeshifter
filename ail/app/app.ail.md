# The app around the gesture

Everything that is not the paste gesture itself: the tray icon and its menu,
the settings window, starting once per user, installing and updating itself,
what each platform needs, and where the data lives.

```
app.ail.md                                                   App (this file)
tray-menu/tray-menu.ail.md                                   TrayMenu: the icon and its menu
tray-menu/problem-report/problem-report.ail.md               ProblemReport: the issue link and "Copy version"
settings-window/settings-window.ail.md                       SettingsWindow: the window, its text and layout
settings-window/change-settings/change-settings.ail.md       ChangeSettings: what each setting does when changed
settings-window/preferences/preferences.ail.md               Preferences: the saved settings, defaults and ranges
settings-window/shortcut-choice/shortcut-choice.ail.md       ShortcutChoice: Win+V on Windows, Ctrl+V on macOS
single-instance/single-instance.ail.md                       SingleInstance: one copy per user, the newest wins
self-install/self-install.ail.md                             SelfInstall: install on first start, uninstall
self-install/start-at-login/start-at-login.ail.md            StartAtLogin: the autostart entry per platform
auto-update/auto-update.ail.md                               AutoUpdate: new releases from GitHub
platform-requirements/platform-requirements.ail.md           PlatformRequirements: Accessibility, X11 and Wayland
data-locations/data-locations.ail.md                         DataLocations: where settings, items and logs live
```

```ail
node App : flow   level: experience
  why   Shapeshifter lives out of sight until the gesture is held, so it needs a
        quiet home: a way to reach its settings, to start with the computer,
        to stay current, and to report a problem without digging.   <- Shapeshifter
  what
    Shapeshifter has no main window. It shows an icon in the system tray (the menu bar on a Mac); the icon's menu
      and the settings window are the only way to reach it.                         refined-by: TrayMenu
    The settings window changes the show delay, how much is remembered, notifications, tips, start at login and
      updates; every change takes effect at once, without a restart.               refined-by: SettingsWindow
    Only one copy runs per user; starting a newer version ends an older one.       refined-by: SingleInstance
    The first start of a downloaded release installs it for the user, without administrator rights, and starts
      it at login.                                                                  refined-by: SelfInstall
    It updates itself from GitHub releases every few hours and restarts when it is not in use.   refined-by: AutoUpdate
    Where the platform limits it (a Mac without Accessibility access, a Wayland session), the settings window
      opens by itself and says so.                                                  refined-by: PlatformRequirements
    Settings, pinned items, the saved history and logs stay on the user's computer, in each platform's usual
      places.                                                                        refined-by: DataLocations
  how
    decide the tray icon is the app's only door: no Dock icon on a Mac, no taskbar button
      because "unknown, confirm with the owner: why no window or Dock icon at all"
    decide when the app cannot start, a small window shows the reason and where the log is, with a "Quit" button
      because otherwise a failed start would be invisible: there is no other window to show it
  copy
    error.title     "Shapeshifter could not start"
    error.details   "Details were written to {log folder}."
    error.quit      "Quit"
  children  TrayMenu, SettingsWindow, SingleInstance, SelfInstall, AutoUpdate, PlatformRequirements, DataLocations
  sources
    README.md, "Good to know" and the two details sections
    issues #48 "Tray icon", #283 "Auto update tweaks", #586 "Be proud: add an About"
    PR #614 (port to .NET 11 and Avalonia), PR #627 (install and update), PR #630, PR #635
    driven on Linux (Xvfb, openbox, xfce4-panel tray) 2026-11-20: tray icon, menu, settings window, install, uninstall
    src/Shapeshifter.Desktop/App.axaml.cs, StartAsync and ShowStartupErrorAsync
    src/Shapeshifter.Desktop/Program.cs, Main (with ShowInDock = false on a Mac)
```
