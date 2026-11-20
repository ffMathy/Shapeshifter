# Starting at login

The entry that starts Shapeshifter when the user signs in, one per platform.

```ail
node StartAtLogin : fn   level: system
  why   A clipboard manager is only useful if it is already running when something is
        copied, so it must start with the user's session.   <- SelfInstall
  what
    type IAutostartService = { IsSupported: boolean, IsEnabled: boolean, SetEnabled(enabled: boolean) -> nothing }
    PlatformServices.CreateAutostart() -> WindowsAutostartService | LaunchAgentAutostartService | XdgAutostartService | UnsupportedAutostartService
    StartCommand.Get() -> list of text   — [the running process's executable path (Environment.ProcessPath)], or [dotnet, Shapeshifter.dll] when run as "dotnet Shapeshifter.dll"
    Per platform (IsEnabled is whether the entry exists; SetEnabled(false) removes it):
      Windows => value "Shapeshifter" under HKCU\Software\Microsoft\Windows\CurrentVersion\Run: each part of the command in double quotes, joined by spaces
      macOS => ~/Library/LaunchAgents/app.shapeshifter.desktop.plist: Label app.shapeshifter.desktop, ProgramArguments = the command,
        RunAtLoad true, ProcessType Interactive
      Linux => $XDG_CONFIG_HOME/autostart/shapeshifter.desktop (~/.config/autostart when unset):
        "[Desktop Entry]", "Type=Application", "Name=Shapeshifter", "Comment=Clipboard manager",
        "Exec=<the command, quoted>", "Terminal=false", "X-GNOME-Autostart-enabled=true"
      anything else => IsSupported false; the settings switch is greyed
    Exec quoting (Linux, Desktop Entry spec): each part in double quotes with \ " ` $ escaped, then every \ doubled again and % doubled
    It is turned on:
      by the installer, on the first start of a fresh install (--first-install); see SelfInstall
      on every start of the installed copy whose entry already exists, so the entry points at the installed file
      by the "Start at login" switch in the settings (on writes the entry for the running file, off removes it)
      --uninstall removes it
    examples
      driven, Linux, installed copy => Exec="/home/…/.local/bin/shapeshifter"
      driven, Linux, the switch in a copy run from ~/Downloads/Shapeshifter-720 => Exec="/home/…/Downloads/Shapeshifter-720"; off => the file is gone
      Linux quoting: the part /opt/My App/run%1 => the text "/opt/My App/run%%1", double quotes included
      Linux quoting: the part a\b => the text "a\\\\b", double quotes included (four backslashes)
  how
    decide the platform's own per-user mechanism: the Run key, a LaunchAgent, an XDG autostart entry
      because "unknown, confirm with the owner" (they came with the port, PR #614)
    decide the switch reads and writes the entry itself rather than a saved setting
      because "unknown, confirm with the owner"
    decide a copy started as "dotnet Shapeshifter.dll" registers dotnet with the dll
      because that build has no executable of its own
  sources
    PR #614 (autostart via the Run key, a LaunchAgent or an XDG entry); PR #627 (a fresh install turns it on)
    driven on Linux 2026-11-20, in a throwaway HOME and XDG_CONFIG_HOME: the entry the installer wrote, and the switch on and off
    src/Shapeshifter.Platform/Common/AutostartServices.cs
    src/Shapeshifter.Desktop/App.axaml.cs, ConfigureInstalledCopy; src/Shapeshifter.Desktop/Services/Installation.cs, RemoveAutostart
```
