# The problem report

The text that "Report a problem" and "Copy version" are made of.

```ail
node ProblemReport : fn   level: system
  why   A bug report is only useful with the exact version and system, which users
        could not find by themselves.   <- TrayMenu
  what
    DescribeEnvironment(appName: text, version: text, operatingSystem: text, architecture: Architecture, displayServer: text or nothing) -> text
    DescribeCurrentEnvironment(appName: text) -> text   — this process: ReleaseVersion.Current(), RuntimeInformation.OSDescription (trimmed), OSArchitecture, and on Linux the display server
    CreateIssueUrl(app: AppIdentity, environment: text) -> text   — an absolute web address
    examples
      DescribeEnvironment("Shapeshifter", "7.1.0", "Linux (Ubuntu 24.04.1 LTS)", X64, "X11") => "Shapeshifter 7.1.0 · Linux (Ubuntu 24.04.1 LTS) · X64 · X11"
      DescribeEnvironment("Shapeshifter", "7.1.0", "Microsoft Windows 10.0.26100", Arm64, nothing) => "Shapeshifter 7.1.0 · Microsoft Windows 10.0.26100 · Arm64"
      DescribeEnvironment(…, displayServer: "  ") => no fourth part (blank counts as none)
      display server: WAYLAND_DISPLAY set => "Wayland"; else DISPLAY set => "X11"; else none
      on the driven Linux machine => "Shapeshifter 7.1.0 · Ubuntu 24.04.5 LTS · X64 · X11"
      CreateIssueUrl(ffMathy/Shapeshifter, env) => "https://github.com/ffMathy/Shapeshifter/issues/new?labels=bug&body=" + the body, percent-encoded (every character but A–Z a–z 0–9 - _ . ~, as EscapeDataString does)
    The issue body is these lines, each ending in a new line:
      "**What happened?**", "", "", "**What did you expect to happen?**", "", "",
      "**Steps to reproduce** (which application you copied from and pasted into)", "", "1. ", "",
      "**Environment**", "", env, "",
      "<!-- If something went wrong, attach the log file (\"Open log folder\" in Shapeshifter's menu). Remove anything private from it first: it can contain window titles. -->"
    Where the report is used:
      tray "Report a problem…" and the settings' "Report a problem" link: Launcher.OpenUrl(CreateIssueUrl(Installation.App, DescribeCurrentEnvironment("Shapeshifter")))
      the settings' "Copy version": the clipboard gets DescribeCurrentEnvironment("Shapeshifter"); the link reads "Copied" for 2 s
      OpenUrl accepts only http and https links and opens them with the desktop's handler (ShellExecute, open, xdg-open); a failure is logged, never shown
  how
    decide the report and the log stay in English, whatever the display language
      because the maintainer reads them (docs/architecture.md, "Languages")
    decide the comment in the body warns that logs can hold window titles
      because the log names the windows items were copied from, which can be private
    decide nothing is uploaded automatically; the user sends the issue from the browser
      because "unknown, confirm with the owner: was automatic error reporting dropped on purpose in the port (#561 mentions it)"
  sources
    issues #342 "Show version number in settings and allow copying", #561 "Open browser with Github issue"
    commit 741b0e5; PR #630
    driven on Linux 2026-11-20: the tray item opened the exact URL above (recorded by a stand-in for xdg-open)
    src/Shapeshifter.Core/Support/ProblemReport.cs
    src/Shapeshifter.Platform/Common/Launcher.cs
    src/Shapeshifter.Desktop/Views/SettingsWindow.axaml.cs, OnCopyVersion
```
