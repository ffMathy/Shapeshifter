# Installing itself

A downloaded release is one file. Its first start installs it for the user and
starts the installed copy; there is no installer to run.

```ail
node SelfInstall : fn   level: system
  why   Downloading one file and starting it should be all it takes, without
        administrator rights, and later downloads must not leave stray copies.   <- App
  what
    type StartupResult = { Exit: boolean, Arguments: list of text, IsInstalledCopy: boolean, FirstInstall: boolean }
    SelfInstaller.OnStartup(args: list of text) -> StartupResult — first thing in Main, never throws
    SelfInstaller.ParseArguments(args: list of text) -> { Remaining: list of text, Replaces: integer or nothing, NoInstall: boolean, Uninstall: boolean, FirstInstall: boolean }
    type InstallLayout = { Target: text, Executable: text, StateFile: text, DataHome: text }
    InstallLayout.Create(app: AppIdentity, platform: InstallPlatform, home: text, localAppData: text, stateDirectory: text, dataHome: text or nothing = nothing) -> InstallLayout
    type RunningApp = { ExecutablePath: text, BundlePath?: text, IsSingleFile: boolean }
    RunningApp.Current() -> RunningApp
    the options are --no-install, --uninstall, --replaces <pid> (from an update) and --first-install (from the installer); all are removed from the arguments
    at start, in order:
      --replaces <pid> => first wait up to 30 s for that process to exit
      not a single-file build (dotnet run, tests, "dotnet Shapeshifter.dll") => run where it is, no install, no update
      leftovers of earlier swaps next to the running file are cleaned
      --uninstall => stop the installed copy, remove its autostart entry, desktop entry or shortcuts, install.json and
        the executable; settings, pinned items, the saved history and logs stay; exit
      a pull request build (version "…-pr.…") not running from the install location => run where it is
      running from the install location => repair the desktop entry or shortcuts, run (FirstInstall = --first-install given)
      --no-install, or SHAPESHIFTER_NO_INSTALL=1 => run where it is
      an installed version newer than this one => start the installed copy with the same arguments, exit
      otherwise => stop the installed copy if it runs, copy this file to the install location (through a temporary
        name, then a rename), write install.json, register, start it (with --first-install when nothing was
        installed before), exit; any failure => log it and run where it is
    install location (InstallLayout.Create for user home H, Windows local app data L)
      Windows => L\Programs\Shapeshifter\Shapeshifter.exe, a Start menu shortcut, and an Apps and Control Panel entry
        under HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Shapeshifter whose uninstall runs "--uninstall"
      macOS => H/Applications/Shapeshifter.app (the running bundle copied with ditto, or a bundle made around the file,
        with LSUIElement so it has no Dock icon); the quarantine mark is removed
      Linux => H/.local/bin/shapeshifter, H/.local/share/applications/shapeshifter.desktop (or $XDG_DATA_HOME/…)
        and the icon in …/icons/hicolor/256x256/apps/shapeshifter.png
      install.json in the data folder: {"version": "7.1.0", "path": "<the installed file>"}
    examples
      ParseArguments(["--no-install", "--replaces", "42", "x"]) => { Remaining: ["x"], Replaces: 42, NoInstall: true, Uninstall: false, FirstInstall: false }
      Create(app, Linux, "/home/u", "", "/home/u/.local/share/shapeshifter").Executable => "/home/u/.local/bin/shapeshifter"
      Create(app, Windows, "C:\Users\u", "C:\Users\u\AppData\Local", s).Executable => "C:\Users\u\AppData\Local\Programs\Shapeshifter\Shapeshifter.exe"
      Create(app, MacOS, "/Users/u", "", s).Executable => "/Users/u/Applications/Shapeshifter.app/Contents/MacOS/Shapeshifter"
      driven, Linux, 7.1.0 from ~/Downloads, nothing installed => installed, started with --first-install, autostart on, the download exits 0
      driven, the same download again while installed 7.1.0 runs => "Ending the running copy", reinstalled, started without --first-install
      driven, 7.2.0 download => replaces 7.1.0; then the 7.1.0 download => "Version 7.2.0 is already installed; starting it instead of 7.1.0"
      driven, --uninstall => executable, desktop entry, icon, autostart entry and install.json gone; settings and logs stay
  how
    decide install for the current user only, in the user's own folders
      because no administrator rights must be needed (PR #627); the old one copied itself to Program Files behind a UAC prompt (#248)
    decide a fresh install turns on start at login
      because the WPF version did (PR #627)
    decide an older download starts the newer installed copy instead of replacing it
      because download sites may link to an older version (#456, PR #627)
    decide the running copy is killed before its file is replaced
      because there is no channel to ask it to quit
    decide only single-file release builds install or update themselves; pull request builds never install
      because dotnet run, tests and the screenshot tool must stay where they are (RunningApp); for pull request builds "unknown, confirm with the owner" (PR #635)
  children  StartAtLogin
  sources
    PR #627 "Install on first start and update from GitHub releases"; PR #635; commits 608e752, cf3fd75; issues #19, #248, #456
    driven on Linux 2026-11-20: single-file 7.1.0 and 7.2.0 published locally, HOME and XDG_* in a throwaway folder (.ail/app/install)
    src/Shapeshifter.Core/Updates/SelfInstaller.cs, InstallLayout.cs, DesktopIntegration.cs, FileSwap.cs
    src/Shapeshifter.Desktop/Services/Installation.cs; README.md, "Where Shapeshifter installs itself"
```
