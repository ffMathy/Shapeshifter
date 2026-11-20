# Actions

Besides pasting an item as it was copied, the picker offers other things to do
with it: its actions, listed to the right of the history. Right moves the choice
into them, Left back to the history, and letting go of Ctrl runs the chosen one.

```ail
node Actions : flow   level: experience
  why   A copied thing is often needed in another shape: without its
        formatting, as a file, or kept for later. Converting it should not need
        another app or a second trip to copy it again (#6, #23, #24).   <- Shapeshifter
  what
    Scenario: Right opens the actions, and letting go of Ctrl runs the chosen one
      Given "memo" was copied
      When the user holds Ctrl+V until the picker appears, taps Right and Down
      And lets go of Ctrl
      Then the picker closes, nothing is typed into the text box
      And the clipboard holds a file "Text <date> <time>.txt" that contains "memo"
    Scenario: Left goes back to the history
      Given "ant", "bee" and "cat" were copied, in that order
      When the user holds Ctrl+V until the picker appears, taps Right, Down, Left and Down
      And lets go of Ctrl
      Then "bee" is pasted, once
    Scenario: choosing another item starts again at Paste
      Given "owl" and then "fox" were copied
      When the user holds Ctrl+V until the picker appears, taps Right and Down, then taps V
      And lets go of Ctrl
      Then "owl" is pasted as it was copied
    Scenario: Escape in the actions cancels without running anything
      Given "calm" was copied
      When the user holds Ctrl+V until the picker appears, taps Right, Down and Escape
      And lets go of Ctrl
      Then nothing is pasted and the clipboard is unchanged
  how
    decide the actions sit to the right of the history and open with Right; Left goes back
      because they moved to the right of the picker, so the arrow points where they are (commit "Open the actions with the Right arrow, now that they are on the right")
      rejected hiding the actions until an arrow is pressed, which #258 planned, because "unknown, confirm with the owner"
    decide while the actions are open, Up and Down move through them and wrap around at both ends
      because "unknown, confirm with the owner: the same wrap-around as the history, on purpose?"
    decide V, a number or Delete leaves the actions: the choice goes back to the history, and moving to another item chooses Paste again
      because "unknown, confirm with the owner: so a choice made for one item never runs on another?"
    decide the chosen action runs when Ctrl is let go, or on Enter, exactly as Paste does
      because "unknown, confirm with the owner"
    decide the first action, Paste, is the default and carries a tick
      because a plain release must paste as before (#573 "Default action never selected")
    decide Right does nothing on an item without actions
      because every item offers at least Paste and Pin today, so this only guards an empty list
  look
    the actions column: a preview card of the chosen item on top, then the heading and a card with one row per action: an icon, the title, a one-line description below it, and a tick on the default
    while the actions are open, the chosen action has the focus ring and the history's row loses it
  copy
    footer.actions   "Actions"     next to the "←→" key in the picker's footer
  uses      GestureRules
  children  OfferedActions, PasteActions, PinItems, DeleteItems, ActionCatalog, UploadImage
  sources
    GitHub issues #6, #23, #24, #258, #573; PRs #629, #634, #644
    commit 1ab814a "Open the actions with the Right arrow, now that they are on the right"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool and xclip: Right, Down, Left, V and Escape in the actions; screenshots of the pane before and after Right
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, OnNavigationKey, Move, Select and DeleteSelected
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, the actions pane
```
