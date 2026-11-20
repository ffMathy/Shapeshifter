# Shapeshifter

A clipboard manager for Windows, macOS and Linux: copy many things, paste any
of them. This spec is being written in reverse from the existing app
(`/ail:adopt`), one slice at a time. So far it covers the paste gesture.

```
shapeshifter.ail.md                                          Shapeshifter (this file)
paste-gesture/paste-gesture.ail.md                           PasteGesture: a quick paste, or hold for the history
paste-gesture/pick-from-history/pick-from-history.ail.md     PickFromHistory: choose an older item, or cancel
paste-gesture/keep-history-open/keep-history-open.ail.md     KeepHistoryOpen: Space, then Enter or Esc
paste-gesture/gesture-rules/gesture-rules.ail.md             GestureRules: the keys, as a pure state machine
```

Not adopted yet: capturing copies, the picker's layout and preview, the
actions, search, pinning, settings, notifications and tips, install and update.

## Target

The app as it is built today. Changing this block, then `/ail:build`, moves it
to another technology; the end-to-end checks in `checks/e2e/` prove the new
app behaves like this one.

```ail
target
  code         C# 14 on .NET 11, nullable enabled, warnings as errors; Avalonia 12 for windows; SharpHook for the global keyboard hook and simulated keys
  structure    Shapeshifter.Core holds every rule as plain, testable code; Shapeshifter.Platform touches the operating system; Shapeshifter.Desktop is the Avalonia app
  naming       PascalCase types and members, one public type per file, file named after the type
  copy         user-visible text comes from Strings.resx and its translations, never from code
  path.fn      src/Shapeshifter.Core/Ail/{dir}/{Pascal}.cs
  path.flow    src/Shapeshifter.Core/Ail/{dir}/{Pascal}.cs
  generated    src/Shapeshifter.Core/Ail/**
  tests        xUnit v3 on Microsoft.Testing.Platform; end-to-end checks are bun tests that drive the desktop with xdotool
```

## Glossary

```ail
glossary
  term history      plain "everything copied while Shapeshifter runs, newest first, pinned items on top"   formal ClipboardHistory
  term item         plain "one thing that was copied"                                                        formal ClipboardItem
  term picker       plain "the window that shows the history while you choose"                              formal PickerWindow
  term "the gesture" plain "holding Ctrl and V to paste, or to choose from the history"                     formal PasteGestureStateMachine
  term "quick paste" plain "Ctrl+V let go before the history appears: an ordinary paste"                    formal PasteCurrentClipboard
  term "show delay" plain "how long V must stay held before the history appears"                            formal ShapeshifterSettings.PasteDelayMilliseconds, 300 ms by default
  term "kept open"  plain "the history stays up without holding Ctrl, after Space"                          formal PickerSelection.IsHeldOpen
  term Ctrl         plain "Ctrl, or Cmd on macOS"                                                            formal KeyMap gesture modifier
```

---

```ail
node Shapeshifter : system   level: product
  why   Copying one thing pushes out the last; people lose what they copied a
        moment ago and go back to copy it again.
  what
    1. A quick Ctrl+V pastes exactly as it would without Shapeshifter.         refined-by: PasteGesture
    2. Holding Ctrl+V shows everything copied, to paste any of it.             refined-by: PasteGesture
    3. You choose with the keys you are already holding, and can cancel.       refined-by: PickFromHistory
    4. The history can stay open to look through it without holding a key.    refined-by: KeepHistoryOpen
  entry     PasteGesture
  children  PasteGesture
  sources
    README.md, sections "Hold Ctrl+V" and "Good to know"
    docs/architecture.md
```
