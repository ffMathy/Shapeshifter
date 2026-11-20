# Keeping the history open

Space turns the gesture into a picker that stays up without holding a key,
for looking through the history at leisure.

```ail
node KeepHistoryOpen : flow   level: experience
  why   Holding Ctrl down while reading through a long history is tiring;
        people asked for a way to keep it open to review it.   <- PasteGesture
  what
    Scenario: Space keeps the history open, and Enter pastes
      Given "oak", "elm" and "ash" were copied, in that order
      When the user holds Ctrl and V, taps Space, and lets go of V and then of Ctrl
      Then the picker stays open
      When the user taps Down, Down, then 2, then Enter, without Ctrl
      Then the picker closes and "elm" is pasted, once
      And no space, digit or new line reaches the text box
    Scenario: Escape closes a history kept open, without pasting
      Given "stay" and then "newest" were copied
      When the user holds Ctrl and V, taps Space, and lets go of V and then of Ctrl
      And taps Escape
      Then the picker closes and nothing is pasted
  how
    decide Space shows the picker at once, even before the show delay
      because the user sees straight away that it stays open (PR #632)
    decide while it is kept open, the arrows, 1 to 9, Delete and Left/Right work without Ctrl; other keys reach the app
      because the user has let go of Ctrl, and keys that are not the picker's must not be lost (PR #632)
    decide Enter pastes and Escape closes; letting go of Ctrl does nothing
      because the picker waits for an explicit choice once it is kept open (#420)
    decide Space, not a separate shortcut
      because it is next to the keys already held, as the latest comment on #420 suggested
      rejected a separate shortcut such as Win+Ctrl+C, which #420 first asked for, because "unknown, confirm with the owner"
  copy
    footer.paste      "Paste"        next to the "Enter" key, while kept open, in place of "Release to paste"
    footer.keepopen   "Keep open"    next to the "Space" key, before Space is pressed
    footer.close      "Close"        next to the "Esc" key, while kept open, in place of "Cancel"
  uses      GestureRules
  sources
    issue #420 "Shortcut to hold overlay open"; PR #632 "Keep the history open with Space, and paste with Enter"
    driven on Linux (Xvfb, openbox) 2026-11-20: screenshot of the kept-open picker, footer "Enter Paste · V ↑↓ Move · 1–9 Jump · ←→ Actions · S Search · Del Remove · Esc Close"
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, the held-open state in OnNavigationKey and OnModifierUp
    src/Shapeshifter.Core/Localization/Strings.resx, PickerHint* and PickerReleaseHintHeldOpen
```
