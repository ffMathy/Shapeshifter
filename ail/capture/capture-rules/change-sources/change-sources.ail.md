# Change sources

How each system tells Shapeshifter that the clipboard changed. None of them
reads the clipboard; that is the monitor's job once the debounce has passed.

```ail
node ChangeSources : fn   level: system
  why   Noticing a copy must cost nothing and never get in another app's
        way, on every system.   <- CaptureRules
  what
    type IClipboardChangeSource = { Start() -> nothing }
    IClipboardChangeSource.Changed — an event, raised with no arguments when the clipboard changed
    PlatformServices.CreateClipboardChangeSource(backend: IClipboardBackend, logger: Logger) -> IClipboardChangeSource
    PollingClipboardChangeSource(tokenProvider: () -> later text, interval: duration, logger: Logger or nothing = nothing) -> PollingClipboardChangeSource
    examples
      Windows => polls GetClipboardSequenceNumber every 150 ms
      macOS => polls NSPasteboard.changeCount every 200 ms
      Linux with XFixes => X11Integration: XFixesSelectSelectionInput on the root window for CLIPBOARD, SetSelectionOwnerNotify; each XFixesSelectionNotify raises Changed; no polling
      Linux without XFixes (or no X display) => polls the backend's change token every 500 ms
      change token => the clipboard's format names joined with "|", then "|" and its text; compared for equality only
      polling, first token read => remembered, no Changed
      polling, token unchanged => no Changed;  token differs => Changed once
      polling, the token read throws => logged, the next tick tries again
      X11, PRIMARY selection (select and middle-click) changes => no Changed
      X11, another app takes the CLIPBOARD selection with the same content => Changed (the history then moves or ignores it)
  how
    decide Windows reads the sequence number instead of listening for clipboard messages
      because it never opens the clipboard, so other applications are never blocked (docs/architecture.md)
    decide Linux uses XFixes selection-owner notices on Shapeshifter's private X connection, falling back to polling
      because notices cost nothing between copies; polling is the fallback where the server has no XFixes (docs/architecture.md)
    decide only the CLIPBOARD selection is watched, not PRIMARY
      because "unknown, confirm with the owner"
    decide the poll intervals: 150 ms on Windows, 200 ms on macOS, 500 ms for the fallback
      because "unknown, confirm with the owner"
    decide the fallback's token is the formats and text themselves, not a hash of them
      because the poller only compares for equality, and a string comparison stops at the first difference (commit b069ac4)
  sources
    src/Shapeshifter.Platform/PlatformServices.cs, CreateClipboardChangeSource
    src/Shapeshifter.Platform/Linux/X11Integration.cs, Start and HandleEvent (XFixes)
    src/Shapeshifter.Core/Clipboard/PollingClipboardChangeSource.cs
    src/Shapeshifter.Desktop/Services/AvaloniaClipboardBackend.cs, GetChangeTokenAsync
    docs/architecture.md, "Capturing copies"
    driven on Linux (Xvfb, openbox) 2026-11-20: the log says "Connected to X11 (trigger keycode 55, XFixes True)"; xclip copies were noticed at once
```
