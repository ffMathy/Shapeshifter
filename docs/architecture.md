# Architecture

Shapeshifter is split into three layers so that almost all behaviour is plain, testable C#, and only a thin layer touches the operating system.

```
Shapeshifter.Desktop (Avalonia app)
 ├─ AvaloniaClipboardBackend ── IClipboardBackend ──────┐
 ├─ PickerPresenter / CopyNotifier ── IPickerPresenter  │
 └─ App (composition root)                              │
                                                        ▼
Shapeshifter.Core                                 Shapeshifter.Platform
 ├─ ClipboardMonitor ◄── IClipboardChangeSource ◄─ Win32 sequence number / NSPasteboard changeCount / X11 XFixes
 ├─ ClipboardHistory                                     
 ├─ ShapeshifterEngine ◄── IKeyboardHook ◄──────── SharpHook global hook (+ X11 key grabs on Linux)
 │    └─ PasteGestureStateMachine                        
 ├─ Actions ──► IPasteSimulator ◄──────────────── SharpHook EventSimulator (Ctrl+V / Cmd+V)
 ├─ PinnedItemStore, SettingsStore, AppPaths     IAutostartService: Run key / LaunchAgent / XDG autostart
```

## Capturing copies

1. The platform's change source signals that the clipboard changed:
   - Windows: `GetClipboardSequenceNumber` polled every 150 ms. It never opens the clipboard, so other applications are never blocked.
   - macOS: `NSPasteboard.changeCount` polled every 200 ms.
   - Linux: XFixes selection-owner notifications on a private X connection, with no polling.
2. `ClipboardMonitor` debounces bursts (applications often write several times per copy) and reads a `ClipboardSnapshot` through Avalonia.
   - It reads text, files and the image through Avalonia's portable formats.
   - It reads every other platform format as raw bytes. `ClipboardFormatClassifier` skips protocol targets and handle-based Windows formats.
3. `ClipboardHistory` inserts the snapshot below the pinned items. If the same content (by `Fingerprint`) is already present, that item moves to the top instead of being duplicated.

Shapeshifter's own clipboard writes go through `ClipboardMonitor.WriteAsync`. Their fingerprint and a short grace period are remembered, so they are not captured as new copies.

## The paste gesture

`PasteGestureStateMachine` is a pure state machine fed with key events and clock ticks. It returns whether to suppress each event and a list of effects: `ShowPicker`, `UpdatePicker`, `HidePicker`, `PerformAction`, `PasteCurrentClipboard`, `DeleteItem` and `GestureCancelled`.

`ShapeshifterEngine` calls it synchronously on the hook thread, because suppression has to be decided before the hook callback returns. The effects then run, in order, on a background worker.

**Suppression:**
- Windows and macOS: the SharpHook callback sets `SuppressEvent`, so the focused application never receives the intercepted V.
- Linux: libuiohook cannot suppress events. The X11 integration therefore holds a passive grab on Ctrl+V for all CapsLock and NumLock combinations, and grabs the navigation keys only while a gesture is active. Key events are still observed through XRecord, which also reports grabbed keys.

**Pasting:**
- If the user did not choose anything else, the current clipboard is pasted untouched (`PasteCurrentClipboard`). This keeps a quick Ctrl+V identical to a normal paste, even for data that was copied a few milliseconds earlier.
- Otherwise the selected action runs. "Paste" restores the snapshot with all of its formats and then sends Ctrl+V (Cmd+V on macOS).
- On Linux, the trigger grab is released while the synthetic keystroke is sent.
- The hook recognises the echo of its own keystrokes by matching the exact sequence it sent, because XRecord does not flag simulated events.

The picker window is created with `ShowActivated=false` and `Topmost=true`. It never takes keyboard focus, so the paste always lands in the application the user was working in. It opens in the top-left corner of the screen that contains the pointer, with the history on the left and the selected item's actions (title and description) on the right, coloured with the operating system's accent colour. Each row is laid out by kind, as in the original design: text shows two lines and the window it was copied from (`IClipboardSourceLocator`, asked when the clipboard change is noticed); an image fills the row; copied files show their system icons (`IFileIconProvider`) with counts and their folder. Every row also shows when it was last copied ("5 min ago", `RelativeTime`) and a pin when it is pinned. With nothing copied yet, holding the gesture shows the picker without items, explaining how to use it (the selection is `IsEmpty`); releasing still pastes normally.

`tools/Shapeshifter.ScreenshotGenerator` renders (at the 125 % display scaling the original screenshots were taken at) the real picker headlessly with sample content to produce `assets/screenshots` (`scripts/generate-screenshots.sh`); CI runs it on pull requests and commits changed screenshots.

## Testing

- `tests/Shapeshifter.Core.Tests` covers the core logic with fakes: the gesture state machine, history, monitor, actions, persistence, settings and the engine.
- `tests/Shapeshifter.EndToEnd.Tests` starts the real application and a separate target application (`tests/Shapeshifter.EndToEnd.TargetApp`, controlled through stdin).
  - It copies through the OS clipboard and drives real keystrokes with SharpHook's simulator.
  - It then checks the exact text that arrived in the target. A paste that leaks through unsuppressed would show up as duplicated text.
  - Shapeshifter reports its state for these tests through the file named by `SHAPESHIFTER_STATE_FILE`.
- On Linux CI the end-to-end tests run under Xvfb with openbox, because a window manager is needed for keyboard focus. They also run on the Windows runner, where the target application also puts exact native image formats on the clipboard (as Snipping Tool and Firefox do) and compares every image format after a paste.
- `.github/workflows/remote-desktop.yml` connects the Windows runner to itself over Remote Desktop and checks that pasting into the remote session works.
- macOS runners cannot grant Accessibility access, so macOS CI runs the build and the unit tests only.
