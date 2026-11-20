# Picking from the history

With the picker up and Ctrl still held, the user chooses an item with keys
next to the ones they already hold, then lets go of Ctrl to paste it.

```ail
node PickFromHistory : flow   level: experience
  why   Choosing must not need the mouse or a second hand, and a look at the
        history must never cost the user their work.   <- PasteGesture
  what
    Scenario: pressing V again picks the next older item
      Given "one", "two" and "three" were copied, in that order
      When the user holds Ctrl+V until the picker appears, lets go of V and taps V once more
      And lets go of Ctrl
      Then the picker closes and "two" is pasted, once
    Scenario: the arrow keys move the choice
      Given "red", "green" and "blue" were copied, in that order
      When the user holds Ctrl+V until the picker appears and taps Down, Down, Up
      And lets go of Ctrl
      Then "green" is pasted, once
    Scenario: a number jumps to that item
      Given "first", "second" and "third" were copied, in that order
      When the user holds Ctrl and V, taps 3 and lets go of V and Ctrl
      Then "first" is pasted, and no "3" is typed
    Scenario: Enter pastes right away
      Given "early" and then "late" were copied
      When the user holds Ctrl+V until the picker appears, taps Down and taps Enter
      Then the picker closes and "early" is pasted while Ctrl is still held
      And letting go of Ctrl pastes nothing more
    Scenario: the pasted item moves to the top
      Given "older" and then "newer" were copied
      And the user picked "older" from the history and pasted it
      When the user makes a quick paste
      Then "older" is pasted again
    Scenario: Escape cancels without pasting
      Given "keep" and then "latest" were copied
      When the user holds Ctrl+V until the picker appears, taps V, then taps Escape
      Then the picker closes
      And letting go of Ctrl pastes nothing
  how
    decide V, Down and Up move the choice; Down and V go to older items
      because V is already under the finger, so choosing needs no other key (README)
    decide the choice wraps around at both ends
      because it was never designed, but it is harmless, so it stays (owner, 2026-11-20)
    decide 1 to 9 jump to the first nine items, counted from the top; a number past the end does nothing
      because "unknown, confirm with the owner"
    decide pasting an item moves it to the top of the history
      because what was just pasted is what a plain paste should repeat (owner, 2026-11-20)
    decide Escape cancels; nothing is pasted, and letting go of Ctrl afterwards does nothing
      because looking for something in the history, or going to copy it again, must not paste by accident (#255)
    decide Enter pastes at once, even while Ctrl is held
      because it pastes without letting go of Ctrl, so the user can go on, e.g. paste again right after (owner, 2026-11-20; added in PR #632)
  copy
    hint.move     "Move"       next to the "V" and "↑↓" keys
    hint.jump     "Jump"       next to the "1–9" key
    hint.cancel   "Cancel"     next to the "Esc" key
    hint.count    "{count} items"
  uses      GestureRules
  sources
    interview with the owner, 2026-11-20: wrap-around, move to top, Enter
    issue #255 "No way to cancel a paste action once overlay is opened"; issue #578
    README.md, "Hold Ctrl+V"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool: V, arrows, 3, 9 with four items (nothing), Up from the newest (wrapped to the oldest), Enter, Escape
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, OnNavigationKey, Move, Wrap
    src/Shapeshifter.Core/Actions/PasteActions.cs, MoveToTop
```
