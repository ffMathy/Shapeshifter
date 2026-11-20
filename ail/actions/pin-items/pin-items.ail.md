# Pinning items

"Pin to clipboard" keeps an item at the top of the history for good: copying
does not push it down, and it is still there after Shapeshifter restarts.

```ail
node PinItems : flow   level: experience
  why   Some things are pasted again and again (an address, a signature, a
        snippet); they should not scroll away or be lost when the computer
        restarts, while the rest of the history stays private to this run.   <- Actions
  what
    Scenario: pinning keeps an item at the top
      Given "keep" and then "later" were copied
      When the user holds Ctrl+V until the picker appears, taps Down, Right and Up, and lets go of Ctrl
      Then the picker closes and nothing is pasted
      When "even later" is copied and the user holds Ctrl+V until the picker appears
      Then "keep" is listed above "even later", with a pin
      And letting go of Ctrl pastes "even later", while tapping 1 instead pastes "keep"
    Scenario: a pinned item offers Unpin, and unpinning lets it move down again
      Given "sticky" was pinned and "next" was copied after it
      When the user chooses "sticky" and taps Right
      Then its actions are "Paste", "Paste as file", "Unpin from clipboard"
      When the user taps Up and lets go of Ctrl, then copies "newest"
      Then "newest" is listed above "sticky", and tapping 1 pastes "newest"
    Scenario: pinned items come back after a restart, the rest does not
      Given "pinned" was pinned, and "gone" and "current" were copied after it
      When the user quits Shapeshifter from its tray menu and starts it again
      And holds Ctrl+V until the picker appears
      Then the history shows "pinned" but not "gone"
      And tapping 1 and letting go of Ctrl pastes "pinned"
    Scenario: a deleted pinned item does not come back
      Given "doomed" was pinned and "other" was copied after it
      When the user chooses "doomed" in the picker and taps Delete, then quits and starts Shapeshifter again
      Then the history does not show "doomed"
  how
    decide pinned items sit above everything else, in the order they were pinned; a new pin goes below the earlier ones
      because pinned items must be in the top, where they are found without looking (#430)
    decide pinned rows show a pin next to when they were copied
      because a pinned item must be told apart from the newest copy (#435, PR #623)
    decide a plain Ctrl+V still pastes the newest copy, not a pinned item; the picker opens on the newest copy
      because pinning must not change what an ordinary paste does
    decide pinning saves the item on the computer straight away; unpinning deletes it from there
      because the action promises it "is there the next time you open Shapeshifter", and Unpin that it "is no longer saved to your computer"
    decide an unpinned item goes back to the top of the history, as if just copied
      because "unknown, confirm with the owner: should it go back where its copy time puts it?"
    decide copying again what a pinned item holds leaves the pinned item where it is, and adds no duplicate
      because "unknown, confirm with the owner"
    decide pinned items do not count towards "Items to remember"
      because they are kept in addition (the setting's own description says so)
    decide nothing but pinned items is written to disk, unless "Remember history after restart" is on
      because "unknown, confirm with the owner"; commit b3f57d8 keeps it so: "by default only pinned items are ever written to disk"
  copy
    action.pin     "Pin to clipboard"
    action.unpin   "Unpin from clipboard"
    tray.quit      "Quit Shapeshifter"   the tray menu's last item, used to quit before a restart
  uses      SavedItems
  children  RememberHistory, SavedItems
  sources
    GitHub issues #430, #435; PR #623 "Show copy times and pins in the picker"
    README.md, "Pinned items survive restarts"; commit b3f57d8
    driven on Linux (Xvfb, openbox, an xfce4-panel tray) 2026-11-20: pinning, the pin on the row, Unpin, quitting from the tray and starting again, deleting a pinned item
    src/Shapeshifter.Core/Actions/PinActions.cs, TogglePinAction
    src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs, SetPinned, AddPinned and Add
    src/Shapeshifter.Desktop/App.axaml.cs, StartAsync: pinned items are loaded before the saved history
```
