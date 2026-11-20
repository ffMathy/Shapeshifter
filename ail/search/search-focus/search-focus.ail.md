# Moving the keyboard focus for a search

The one time Shapeshifter takes the keyboard focus, and how it gives it back
on each platform, so the paste lands in the window the user came from.

```ail
node SearchFocus : fn   level: system
  why   Typing a query needs the focus in the picker, yet the paste must land in
        the app the user was in; a focus left in the wrong place loses the paste.   <- SearchHistory
  what
    type FocusedWindow = { Handle: integer }
      Handle is a window handle on Windows and X11, a process id on macOS
    IFocusedWindowTracker.Capture() -> FocusedWindow or nothing — must not throw; nothing when unknown or Shapeshifter's own
    IFocusedWindowTracker.RestoreAsync(window: FocusedWindow) -> later boolean — can be cancelled; true once the window has the focus, false after the wait
    IWindowFocus.ActivateOwnWindow(nativeHandle: integer) -> nothing — called on the UI thread; an IWindowFocus is also an IFocusedWindowTracker
    UiThreadWindowFocus(inner: IWindowFocus) -> IWindowFocus — runs RestoreAsync on the UI thread, whatever thread asks
    FocusWait.Timeout: duration = 600 ms — polled every 15 ms
    PickerPresenter.ActivationTimeout: duration = 1.5 s
    a search moves the focus in this order:
      BeginSearch: Capture() first (the picker has not taken the focus), then PickerPresenter.BeginSearchAsync: empty query, search bar shown, ActivateOwnWindow, Activate, focus the search box
      until the search ends, the keyboard hook passes every key through, tracking only the modifiers and the release of V
      Enter: RestoreFocus runs, and is awaited, before HidePicker and PerformAction
      a failed restore logs "Could not give the keyboard focus back to the window it came from" and the action still runs
    on each platform:
      Windows   Capture: GetForegroundWindow, nothing for Shapeshifter's own; Activate: AttachThreadInput to the foreground thread, then SetForegroundWindow; Restore: the same, then wait for GetForegroundWindow
      macOS     Capture: the frontmost application's process id; Activate: activateIgnoringOtherApps:; Restore: activateWithOptions: (ignoring other apps), then wait for it to be frontmost
      X11       Capture: the focused window (1 s at most); Activate and Restore: _NET_ACTIVE_WINDOW as a pager, else XSetInputFocus and wait 300 ms more
    examples
      picker not activated 1.5 s after BeginSearch => PickerSearch gets Deactivated => [HidePicker, GestureCancelled]
      picker hidden before BeginSearchAsync runs => Deactivated at once
      picker deactivated after it had the focus (a click elsewhere) => Deactivated; the focus stays where the user put it
      picker deactivated before it ever had the focus => ignored, the 1.5 s timer decides
      BeginSearchAsync throws => the search ends, the hook stops passing keys through, the picker hides
      on X11, Ctrl+V while searching => swallowed by the root-window grab of Ctrl+V, never reaches the search box
  how
    decide give up after 1.5 s without the focus
      because otherwise the keys still go to the other app, where Escape could not close the picker (PickerPresenter)
    decide restore the focus before pasting, and wait for it
      because the focus goes back while Shapeshifter is still in front and allowed to give it away (docs/architecture.md)
    decide restore on the UI thread
      because on Windows only the thread whose window is in front reliably hands the focus on; from the effect loop SetForegroundWindow was refused at times and the paste went nowhere (commit f16712c)
      rejected waiting 150 ms after the restore because it was based on a wrong guess (commit 0eb19c7, replaced by f16712c)
    decide pass every key through while searching, but keep tracking the modifiers and the release of V
      because V is often still held when S begins the search, and a V thought held would swallow the next Ctrl+V as an auto-repeat (ShapeshifterEngine.OnKey)
    decide on X11, S is grabbed during a gesture with the other navigation keys
      because X11 can only suppress keys grabbed beforehand (IKeyboardHook.SetNavigationCaptureEnabled)
  sources
    src/Shapeshifter.Core/Platform/IKeyboardHook.cs, IFocusedWindowTracker, IWindowFocus, FocusedWindow
    src/Shapeshifter.Desktop/Services/UiThreadWindowFocus.cs; src/Shapeshifter.Desktop/Services/PickerPresenter.cs, BeginSearchAsync, CreateWindow
    src/Shapeshifter.Platform/Windows/WindowsWindowFocus.cs; src/Shapeshifter.Platform/MacOS/MacWindowFocus.cs; src/Shapeshifter.Platform/Linux/X11WindowFocus.cs; src/Shapeshifter.Platform/Common/FocusWait.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, OnKey, OnSearchInput, RestoreFocus, BeginSearchAsync
    src/Shapeshifter.Platform/Input/SharpHookKeyboardService.cs and Linux/X11Integration.cs, the Ctrl+V grab
    commits f709ea8, 0eb19c7, f16712c; PR #635 (macOS not covered by end-to-end tests)
    driven on Linux (Xvfb, openbox) 2026-11-20: the picker became the active window on S; the target app was active again after Enter and Escape; a click on the target closed the picker
```
