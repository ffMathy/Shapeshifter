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
 └─ HistoryPersistence
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

Pinned items are saved by `PinnedItemStore`, one folder per item. With "Remember history after restart" on, `HistoryPersistence` keeps the unpinned items in the same format in a `History` folder, plus `order.json` with their order: each change of the history schedules one background sync that writes new items, deletes removed ones and rewrites the order. Turning the setting off deletes the folder.

Shapeshifter's own clipboard writes go through `ClipboardMonitor.WriteAsync`. Their fingerprint and a short grace period are remembered, so they are not captured as new copies.

## Coaching tips

The history stays out of sight until the paste gesture is held, so new users easily forget it is there. `CopyCoach` watches copies and plain pastes for two habits the history could have saved, and the engine then shows a tip through `ICopyNotifier.NotifyTip` instead of the "copied" notification:

- **Copied again** (#284): content already in the history was copied again, and it was not what the clipboard already held (a second Ctrl+C to be sure is not coached).
- **Back and forth** (#310): the user copied from one application, pasted with a quick Ctrl+V into another, and copied from the first application again within 30 seconds of the first copy.

Applications are compared by name (`ClipboardSource.ApplicationName`; for the paste target, `IClipboardSourceLocator` is asked when the paste runs). Opening the picker ends a back-and-forth sequence, the clipboard content at startup is never coached, and tips are at least five minutes apart. The "Tips" setting turns them off.

## The paste gesture

`PasteGestureStateMachine` is a pure state machine fed with key events and clock ticks. It returns whether to suppress each event and a list of effects: `ShowPicker`, `UpdatePicker`, `HidePicker`, `PerformAction`, `PasteCurrentClipboard`, `DeleteItem` and `GestureCancelled`.

`ShapeshifterEngine` calls it synchronously on the hook thread, because suppression has to be decided before the hook callback returns. The effects then run, in order, on a background worker.

**Suppression:**
- Windows and macOS: the SharpHook callback sets `SuppressEvent`, so the focused application never receives the intercepted V.
- The "Shortcut" setting (#28) offers an alternative to the paste keys: Win+V on Windows, Ctrl+V on macOS. `KeyMap` then makes that key the gesture's `Modifier` and the paste key an `OtherModifier`, while pasting still sends Ctrl+V (Cmd+V on macOS). Windows opens the Start menu when the Windows key is released without another key in between, and it never saw the suppressed V; so the first time a key is suppressed while the Windows key is held, `SharpHookKeyboardService` sends a Ctrl press to "mask" it. X11 has no alternative: Super+V belongs to the desktop.
- Linux: libuiohook cannot suppress events. The X11 integration therefore holds a passive grab on Ctrl+V for all CapsLock and NumLock combinations, and grabs the navigation keys only while a gesture is active. Key events are still observed through XRecord, which also reports grabbed keys.

**When to paste (issue #639):** with the standard shortcut, a quick Ctrl+V pastes as soon as V comes up, before the picker shows, even while Ctrl is still held: the user can hold Ctrl and press V, Down, V, Down… as without Shapeshifter. The history therefore appears only while V is held for the show delay; once it is up, releasing V does nothing, further V presses move the selection and releasing Ctrl pastes. The alternative shortcuts (Win+V, Ctrl+V on macOS) cannot paste while their modifier is held (Win+Ctrl+V is another shortcut), so with them the paste waits for the modifier's release and the history also appears when only the modifier is kept held (`IKeyboardHook.GestureModifierPastes`, `PasteGestureStateMachine.PasteOnTriggerRelease`). While the user holds the paste modifier, `SharpHookKeyboardService` sends only V: a synthetic release of Ctrl would leave Windows and X11 treating it as up while it is held. On macOS it sends Cmd as well, because libuiohook sets a simulated key's flags from the modifiers it simulated itself.

**Pasting:**
- If the user did not choose anything else, the current clipboard is pasted untouched (`PasteCurrentClipboard`). This keeps a quick Ctrl+V identical to a normal paste, even for data that was copied a few milliseconds earlier.
- Otherwise the selected action runs. "Paste" restores the snapshot with all of its formats and then sends Ctrl+V (Cmd+V on macOS).
- "Upload and paste link" is the only action that sends content off the computer. It uploads straight away, without asking, and pastes the link.
- On Linux, the trigger grab is released while the synthetic keystroke is sent.

**Keeping the picker open:** Space during a gesture holds the picker open (`PickerSelection.IsHeldOpen`), for reviewing the history without holding the modifier (issue #420). Releasing the modifier then does nothing; the navigation keys work on their own, Enter performs the selection and Escape closes the picker. Enter also pastes right away in a normal gesture. On Linux the navigation keys (including Space and Enter) are therefore grabbed with and without Ctrl while a gesture is active.
- The hook recognises the echo of its own keystrokes by matching the exact sequence it sent, because XRecord does not flag simulated events.

The picker window is created with `ShowActivated=false` and `Topmost=true`. It never takes keyboard focus, so the paste always lands in the application the user was working in. It opens centred near the top of the screen that contains the pointer: a header, the history on the left, on the right a preview of the selected item above its actions (icon, title and description), and the keys in a footer. The preview (issue #54) is a card that grows with its content up to a maximum height, with a footer strip naming what it is: the first lines of a text (code and commands in a monospaced font), the image itself (only the selected image is decoded at that size), a colour's swatch and code, a file's icon and name, or the icons and names of several files; it is left out when the picker would not fit on the screen with it. A text that is nothing but a link to an image, or the path of an image file (`LinkedImage`, issue #21), shows the image filling the card, with a text icon and the link over it; its row keeps the text icon. `LinkedImageLoader` loads the image (at most 10 MB, remembering the last 16) only once the item is selected, so the picker contacts a site only for the link the user is looking at. Each row has a tile showing what kind of item it is (a thumbnail of an image, the system icon of a file or up to four of several files (`IFileIconProvider`), or a line icon), the content, and the window it was copied from (`IClipboardSourceLocator`, asked when the clipboard change is noticed) or the folder of copied files. On the right, every row shows when it was last copied ("5 min ago", `RelativeTime`) and a pin when it is pinned. With nothing copied yet, holding the gesture shows the picker without items, explaining how to use it (`IsEmpty`); releasing still pastes normally.

### Searching the history

Pressing <kbd>S</kbd> during the gesture emits `BeginSearch` (after `ShowPicker` if the picker was not shown yet) and ends the gesture; the picker stays open. This is the only time the picker takes the keyboard focus, because typing a query needs a real text box (every keyboard layout, dead keys and input methods), which the keyboard hook cannot provide.

1. When the engine processes `BeginSearch`, it asks `IFocusedWindowTracker` which window has the focus (the picker does not have it yet), and creates a `PickerSearch`: a pure state machine over the gesture's items and their search text (`HistorySearch`: the whole text, the plain text of HTML, file paths, the kind and where it was copied from; cached per item, since content never changes).
2. `PickerPresenter.BeginSearchAsync` shows the search bar below the header and activates the picker. Another application is in the foreground, so it goes through `IWindowFocus.ActivateOwnWindow`: on Windows `AttachThreadInput` to the foreground thread and `SetForegroundWindow`, on macOS `activateIgnoringOtherApps:`, on X11 `_NET_ACTIVE_WINDOW` and `XSetInputFocus`. If the picker has not been activated after 1.5 seconds, the search is cancelled.
3. Until the search ends, the keyboard hook passes every key through (Ctrl+V pastes into the search box) and only tracks the modifiers. The window reports the query, the arrows, Enter and Escape to `PickerSearch`, which filters (every word must occur, ignoring case and accents) and moves the selection. Right opens the actions once the caret is at the end of the query. Giving the focus back runs on the UI thread (`UiThreadWindowFocus`): on Windows only the thread whose window is in the foreground can reliably hand it on, and from the effect loop's thread `SetForegroundWindow` was refused at times, so the paste went nowhere.
4. <kbd>Enter</kbd> emits `RestoreFocus`, `HidePicker` and `PerformAction`: the focus goes back first, while Shapeshifter is still the foreground application and allowed to give it away (Windows: `SetForegroundWindow`; macOS: `activateWithOptions:`; X11: `_NET_ACTIVE_WINDOW` as a pager, else `XSetInputFocus`), and the engine waits until the window has it before pasting. <kbd>Esc</kbd> restores the focus and cancels. If the picker loses the focus to another window, the search is cancelled and the focus stays where the user put it.

## Looks

Shapeshifter matches the design language of the operating system it runs on. `PlatformLook` picks a look at startup (the `SHAPESHIFTER_LOOK` environment variable overrides it: `windows`, `macos` or `linux`):

| Look | Platform | Style |
|---|---|---|
| Mica Sheets | Windows 11 | Fluent 2, light and dark: Mica everywhere (the flyout, the notification, a Settings-app window), the accent as a pill on the selected row |
| Refraction | macOS | Liquid Glass: a translucent sheet, a capsule selection, a template menu-bar icon |
| Boxed Sheet | Linux (every desktop) | GNOME libadwaita: boxed lists, Adwaita spacing and switches, a symbolic tray icon |

The windows (`Views/*.axaml`) share one layout with semantic style classes; each look is a style file (`Styles/<Look>.axaml`) over that layout plus its icons (`Assets/<Look>`). Every look takes the operating system's accent colour (`AccentPalette`) and falls back to its own default.

The Windows look follows the app mode of Windows (light or dark), live: `PlatformLook` leaves the theme variant at `Default` for it, so Avalonia reads the system's and restyles open windows (and their DWM title bars) when it changes; `SHAPESHIFTER_THEME=light|dark` overrides it. Its colours are tokens (`Mica…Brush`) in the `Default` and `Dark` theme dictionaries of `Styles/Windows.axaml`, always used through `DynamicResource`, and `AccentPalette` puts the accent's text shade in the application's theme dictionaries (darkened to read on white in light, Fluent's lighter shade in dark). The macOS and Linux looks are light only.

`tools/Shapeshifter.ScreenshotGenerator` renders the real picker of every look headlessly, at 125 % display scaling, with sample content to produce `assets/screenshots` (`windows`, `macos` and `linux`, the Windows look in four accent colours and with the actions chosen, and each look's settings window, copy notification and empty picker) (`scripts/generate-screenshots.sh`). CI renders each look on its own operating system (the Windows look on a Windows runner, and so on), so the screenshots show the font the app really uses there: Segoe UI, SF Pro and Adwaita Sans (`scripts/install-adwaita-fonts.sh`). It runs on pull requests and commits changed screenshots; `--native-fonts` makes it fail rather than render a look in a fallback font.

The headless renders have no system blur or accent colour. With `--live`, the same tool shows the real windows on the screen (through the app's own `PickerPresenter` and `CopyNotifier`), captures it, and writes `diagnostics.txt` with the system's and the app's theme, the transparency level and backdrop granted, whether the system material is on and the fonts resolved. The **Live screenshots** workflow (`.github/workflows/live-screenshots.yml`) runs it on macOS and Windows runners for pull requests that touch the looks, and on demand with a choice of runner images and the macOS appearance; it captures each look as it ships (on Windows in the light and the dark app mode), without the system material, and on macOS with Liquid Glass, and uploads the captures as artifacts.

The picker and the copy notification sit on the operating system's own material where it can be had (`NativeMaterial`): on Windows 11 Mica (Avalonia's, which unlike the system's stays translucent while the window is not active, as these never are), or Acrylic where Mica cannot be had (`SHAPESHIFTER_NATIVE_MATERIAL=acrylic` forces it), with DWM's rounded corners and shadow; and on macOS a frosted popover material (or, with `SHAPESHIFTER_NATIVE_MATERIAL=glass`, Liquid Glass on macOS 26) inserted behind the sheet by `MacWindowMaterial`. Both systems put the material under the whole window, so the window is then exactly the panel. The settings window gets Mica on Windows 11: from 22H2 (build 22621) the system's own (`DWMWA_SYSTEMBACKDROP_TYPE`, set by `WindowsWindowMaterial`), which DWM draws under the native title bar and, with the frame extended, under the transparent client area, as in the Settings app; before 22H2, or if that fails, Avalonia's emulated Mica in the client area only (also with `SHAPESHIFTER_NATIVE_MATERIAL=emulated`). The cards are then translucent layers on it. With Windows' "Transparency effects" off there is no Acrylic or Mica (Avalonia would still report them), and the windows use the look's opaque, theme-coloured surfaces. The setting is read when a window is set up and again each time the picker or the copy notification is shown (turned off, the picker keeps the opaque panel until the app restarts). Battery saver and Energy saver also turn Windows' transparency off without changing that setting: the settings window's system Mica follows them (DWM draws it), but the picker and the copy notification then show Avalonia's Acrylic as an untinted blur under the look's nearly opaque tint; this is a known limit. In a high-contrast theme there is no Acrylic or Mica either; the looks have no high-contrast colours of their own yet. In dark mode the accent shade for text and toggles is Windows' own "Light 2" (Explorer's `AccentPalette`), as in the Settings app, lightened further only where it would not read. `SHAPESHIFTER_NATIVE_MATERIAL=off` turns the system material off.

## Languages

All user-facing text comes from the `Strings` class generated from `src/Shapeshifter.Core/Localization/Strings.resx`
(English) and its translations, which build into satellite assemblies (carried inside a single-file build). The text
follows the current UI culture, the operating system's display language unless `SHAPESHIFTER_LANGUAGE` names another
(`Localizer.ApplyLanguageOverride`, called first thing in `Program.Main`). Views use `{x:Static loc:Strings.…}`; code
fills in format strings with `Localizer.Format`. Actions are recognised by their `Id`, never by their translated title
(`Glyphs.ForAction`). In a right-to-left language (Arabic, Persian) the windows set `FlowDirection.RightToLeft`, which
mirrors their layout. Log messages and the problem report stay in English.

## Testing

- `tests/Shapeshifter.Core.Tests` covers the core logic with fakes: the gesture state machine, history, monitor, actions, persistence, settings and the engine.
- `tests/Shapeshifter.EndToEnd.Tests` starts the real application and a separate target application (`tests/Shapeshifter.EndToEnd.TargetApp`, controlled through stdin).
  - It copies through the OS clipboard and drives real keystrokes with SharpHook's simulator.
  - It then checks the exact text that arrived in the target. A paste that leaks through unsuppressed would show up as duplicated text.
  - Shapeshifter reports its state for these tests through the file named by `SHAPESHIFTER_STATE_FILE`.
- On Linux CI the end-to-end tests run under Xvfb with openbox, because a window manager is needed for keyboard focus. They also run on the Windows runner, where the target application also puts exact native image formats on the clipboard (as Snipping Tool and Firefox do) and compares every image format after a paste.
- `.github/workflows/remote-desktop.yml` connects the Windows runner to itself over Remote Desktop and checks that pasting into the remote session works.
- macOS runners cannot grant Accessibility access, so macOS CI runs the build and the unit tests only.
