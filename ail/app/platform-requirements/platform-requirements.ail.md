# What each platform needs

The gesture needs to see and send keystrokes system-wide. Where the platform
does not allow that, the user is told, in the settings window, at start.

```ail
node PlatformRequirements : fn   level: system
  why   A gesture that silently does nothing looks like a broken app; people must be
        told what is missing and how to fix it.   <- App
  what
    PlatformServices.CreateAsync(logger: Logger) -> later PlatformServices — throws when the platform cannot work at all
    PlatformServices.Warnings: list of text — limits of this session, shown in the settings window
    PlatformServices.PlatformName: "Windows" | "macOS" | "Linux (X11)" | "Linux (Wayland)" | text — text is the OS name for any other system
    what CreateAsync finds, by platform:
      Linux, no X11 display reachable => cannot start: "Shapeshifter needs an X11 display (DISPLAY is not set). On Wayland, make sure XWayland is available."
      Linux, WAYLAND_DISPLAY set or XDG_SESSION_TYPE=wayland => runs through XWayland (XRecord and XTest) with the warning
        "This is a Wayland session. Wayland does not let applications observe or synthesize global keystrokes, so the paste gesture only works while an X11 (XWayland) application is focused. Log in to an X11 session for full support."
      macOS without Accessibility access => the warning "Shapeshifter needs Accessibility access to see and send the Cmd+V keystroke. Allow it in System Settings > Privacy & Security > Accessibility and restart Shapeshifter."
        is added; but starting the keyboard hook then shows the system's own Accessibility prompt and fails with the same
        text, so the start ends in the "Shapeshifter could not start" window with it (exit code 3) before the settings
        window could open; there is no automatic retry once access is granted
      Windows => no requirement, no warning
      any other system => cannot start: "{system} is not supported."
    at start (App.StartAsync), once the tray icon exists and SHAPESHIFTER_READY is printed
      Warnings not empty => each is logged, and the settings window opens by itself with them in a yellow card above
        the settings, separated by a blank line (not when SHAPESHIFTER_STATE_FILE is set, for the end-to-end tests)
      a start that throws => the "Shapeshifter could not start" window with the message and the log folder, then exit code 3
    examples
      Linux, X11, WAYLAND_DISPLAY unset, XDG_SESSION_TYPE=x11 => Warnings [], PlatformName "Linux (X11)"
      driven, Linux, DISPLAY=:105 and WAYLAND_DISPLAY=wayland-0 => PlatformName "Linux (Wayland)"; the settings window opened by itself with the Wayland card
      macOS, Accessibility not granted => Warnings [the Accessibility text]; then SharpHookKeyboardService.StartAsync throws InvalidOperationException(the same text)
  how
    decide on Wayland, run through XWayland with a warning rather than refuse to start
      because XRecord and XTest work through XWayland without root access, for X11 apps (PlatformServices)
    decide Linux suppresses Ctrl+V with X11 key grabs, since libuiohook cannot suppress there
      because otherwise the app would receive the V of every gesture (docs/architecture.md)
    decide the warnings open the settings window instead of a dialog of their own
      because "unknown, confirm with the owner"
    decide on macOS a missing Accessibility access stops the start with the error window
      because "unknown, confirm with the owner: keep this? the settings warning for it can then never be seen"
    decide macOS may need Accessibility granted again after an update
      because macOS ties the permission to the ad-hoc signature of the file (PR #627, a known limit)
  sources
    README.md, "Good to know": "macOS needs Accessibility access. Linux works fully on X11; on Wayland only while an X11 app is focused."
    PR #614, PR #627; docs/architecture.md, "The paste gesture"
    driven on Linux (Xvfb, openbox) 2026-11-20 with WAYLAND_DISPLAY=wayland-0: screenshot of the warning card
    src/Shapeshifter.Platform/PlatformServices.cs, CreateAsync and PlatformName
    src/Shapeshifter.Platform/Input/SharpHookKeyboardService.cs (Accessibility); src/Shapeshifter.Desktop/App.axaml.cs, StartAsync
    src/Shapeshifter.Core/Localization/Strings.resx, Platform*
    not driven: macOS and Windows
```
