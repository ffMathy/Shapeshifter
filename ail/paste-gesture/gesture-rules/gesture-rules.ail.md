# Gesture rules

The gesture as a pure state machine: key events and clock ticks in, whether to
swallow each key and a list of effects out. Everything that touches the
operating system (the hook, the picker window, the paste keystroke) runs the
effects; nothing here does.

```ail
node GestureRules : fn   level: system
  why   The timing and key rules decide whether a paste lands once, twice or
        not at all, so they must be exact and testable without a desktop.   <- PasteGesture
  what
    PasteGestureStateMachine(session: () -> { items: list of GestureItem, defaultIndex: integer }) -> PasteGestureStateMachine
    type GestureResult = { suppress: boolean, effects: list of GestureEffect }
    PasteGestureStateMachine.Handle(input: KeyInput, now: duration) -> GestureResult
    PasteGestureStateMachine.Tick(now: duration) -> GestureResult   — shows the picker once the show delay has passed
    PasteGestureStateMachine.Cancel() -> GestureResult
    ShowDelay: duration = 300 ms
    PasteOnTriggerRelease: boolean = true
    type GestureEffect = { kind: "ShowPicker", sel: PickerSelection } | { kind: "UpdatePicker", sel: PickerSelection } | { kind: "HidePicker" }
      | { kind: "PerformAction", item: text, action: integer } | { kind: "PasteCurrentClipboard" } | { kind: "DeleteItem", item: text }
      | { kind: "GestureCancelled" } | { kind: "BeginSearch", sel: PickerSelection } | { kind: "RestoreFocus" }
    In the examples, keys(...) feeds key events at the given ms; ↓ is a press, ↑ a release; tick(t) advances the clock; items: n means a history of n items with defaultIndex 0.
    examples
      keys(items: 3, Ctrl↓ 0, V↓ 0, V↑ 50) => effects [PasteCurrentClipboard]; V↓ and V↑ suppressed; Ctrl passes through
      keys(items: 3, Ctrl↓ 0, V↓ 0, V↑ 50, V↓ 100, V↑ 150) => [PasteCurrentClipboard], then [PasteCurrentClipboard]
      keys(items: 3, Ctrl↓ 0, V↓ 0, Ctrl↑ 40, V↑ 60) => [PasteCurrentClipboard] on Ctrl↑; V↑ suppressed
      keys(items: 3, V↓ 0) => passes through, no effects
      keys(items: 3, Ctrl↓ 0, Shift↓ 0, V↓ 0) => passes through, no effects
      keys(items: 3, Ctrl↓ 0, V↓ 0, tick 299) => no effects
      keys(items: 3, Ctrl↓ 0, V↓ 0, tick 300) => [ShowPicker(selected 0)]
      keys(items: 3, Ctrl↓ 0, V↓ 0, tick 300, V↑ 350, Ctrl↑ 400) => [HidePicker, PasteCurrentClipboard]
      keys(items: 3, Ctrl↓ 0, V↓ 0, tick 300, V↑ 350, V↓ 400, Ctrl↑ 500) => [UpdatePicker(selected 1)], then [HidePicker, PerformAction(item 1, action 0)]
      keys(items: 3, picker shown on 0, Up) => UpdatePicker(selected 2)
      keys(items: 3, picker shown on 2, Down) => UpdatePicker(selected 0)
      keys(items: 3, picker shown, 3↓) => UpdatePicker(selected 2); keys(items: 3, picker shown, 9↓) => no effects, suppressed
      keys(items: 3, picker shown, Escape↓) => [HidePicker, GestureCancelled]; a later Ctrl↑ => no effects
      keys(items: 3, picker shown on 1, Enter↓) => [HidePicker, PerformAction(item 1, action 0)]
      keys(items: 3, Ctrl↓ 0, V↓ 0, Space↓ 10) => [ShowPicker(heldOpen)]; then Ctrl↑ => no effects; then Down↓ => [UpdatePicker(selected 1)]
      keys(items: 0, Ctrl↓ 0, V↓ 0, tick 300) => [ShowPicker(empty)]; then Ctrl↑ => [HidePicker, PasteCurrentClipboard]
      keys(items: 3, pinned 2, so defaultIndex 2, Ctrl↓ 0, V↓ 0, tick 300) => [ShowPicker(selected 2)]
      keys(gesture ended, V still held, V↓ auto-repeat) => suppressed, no effects
  how
    decide the machine is pure and called synchronously on the hook thread; effects run in order on a background worker
      because suppression must be decided before the hook callback returns, and a slow paste must not stall the keyboard
    decide pasting performs an action only when the choice moved from defaultIndex or the action changed; otherwise PasteCurrentClipboard
      because restoring a snapshot is not byte-identical to what the source app put on the clipboard
    decide auto-repeats of a V whose press started an ended gesture are swallowed
      because the key-down was suppressed, so its repeats must not type "v" or start another gesture (#527)
    decide the selection starts on the newest unpinned item (ClipboardHistory.GetDefaultIndex)
      because pinned items sit on top, yet a plain paste must still mean the latest copy
    decide with PasteOnTriggerRelease off (Win+V on Windows, Ctrl+V on macOS), V↑ does not paste; only the modifier's release does
      because pasting while Win is held would send Win+Ctrl+V, another shortcut (PR #643)
  verify
    property: for any key sequence, at most one of PasteCurrentClipboard or PerformAction is emitted per gesture
    property: HidePicker is emitted only after a ShowPicker in the same gesture
  sources
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, CreateSession
    src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs, GetDefaultIndex
    tests/Shapeshifter.Core.Tests (QuickPasteTests, HoldOpenTests, property tests), as named in PRs #632 and #643
```
