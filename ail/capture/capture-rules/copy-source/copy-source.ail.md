# Where a copy came from

The app and window in front when a copy is noticed. The picker shows it on
each row, and the tips compare apps by its name.

```ail
node CopySource : fn   level: system
  why   "Copied from the browser" helps the user find an item, and the
        back-and-forth tip needs to know which app is which.   <- CaptureRules
  what
    type ClipboardSource = { ApplicationName?: text, WindowTitle?: text, IconPng?: bytes }
    type IClipboardSourceLocator = { GetCurrentSource() -> ClipboardSource or nothing }
    IClipboardSourceLocator.GetCurrentSource() never throws, and gives nothing when the source is unknown.
    examples
      X11, active window of another process => (desktop entry's Name, else WM_CLASS class, else the process's command or executable; _NET_WM_NAME, else WM_NAME; _NET_WM_ICON as PNG, else the desktop entry's themed icon at 32 px)
      X11, the target app's window => ("Shapeshifter.EndToEnd.TargetApp", "Shapeshifter E2E Target", …)    driven
      X11, xlogo's window => ("XLogo", "xlogo", …)    driven
      X11, no _NET_ACTIVE_WINDOW (no EWMH window manager) => nothing
      X11, the active window is Shapeshifter's own => nothing
      X11, neither a title nor an application name => nothing
      Windows, foreground window => (the executable's FileDescription, else ProductName; the window title if visible; the window's icon, else the executable's)
      Windows, a UWP app's frame (ApplicationFrameHost) => the hosted app's process instead
      macOS => (frontmostApplication.localizedName, the front window's title, the app's icon); own process => nothing
      a copy noticed => the locator is asked before the debounce, on the capture's thread
      an item copied again => keeps the source of its first copy, unless it had none (content present at startup)
      a quick paste => the locator is asked for the paste target, before the keystroke (CoachRules)
  how
    decide X11 uses a short-lived XCB connection per call, under a lock
      because X errors (a window that closed a moment ago) then come back per request instead of through Xlib's process-wide handler, whose default ends the process
    decide on X11 the application is named from WM_CLASS through the desktop entries, and the process only when WM_CLASS is missing
      because WM_CLASS identifies the application best; the process may be an interpreter or a wrapper, e.g. python3
    decide all property requests are sent before any reply is read
      because one round trip to the X server keeps the copy path fast
  sources
    src/Shapeshifter.Platform/Linux/X11SourceServices.cs, X11ClipboardSourceLocator
    src/Shapeshifter.Platform/Windows/WindowsSourceServices.cs, WindowsClipboardSourceLocator
    src/Shapeshifter.Platform/MacOS/MacSourceServices.cs, MacClipboardSourceLocator
    src/Shapeshifter.Core/Platform/IClipboardSourceLocator.cs
    src/Shapeshifter.Core/Clipboard/ClipboardMonitor.cs, LocateSource; ClipboardHistory.cs, Add
    docs/architecture.md, "The paste gesture" (rows show the window a copy came from)
    driven on Linux (Xvfb, openbox) 2026-11-20: the back-and-forth tip named "Shapeshifter.EndToEnd.TargetApp" and "XLogo"
```
