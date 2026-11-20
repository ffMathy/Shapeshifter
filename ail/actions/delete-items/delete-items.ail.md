# Deleting an item

Delete, while the picker is up, takes the chosen item out of the history.

```ail
node DeleteItems : flow   level: experience
  why   Things copied by mistake, or that should not stay around such as a
        password, clutter the history and are one wrong key away from being
        pasted; the user needs to get rid of them where they see them.   <- Actions
  what
    Scenario: Delete removes the chosen item
      Given "keep", "drop" and "top" were copied, in that order
      When the user holds Ctrl+V until the picker appears, taps Down and taps Delete
      Then "drop" is gone from the history and the item count is one less
      When the user taps Escape, lets go of Ctrl, and holds Ctrl+V again and taps Down
      And lets go of Ctrl
      Then "keep" is pasted
    Scenario: after Delete the next item is chosen, and letting go of Ctrl pastes it
      Given "first", "second" and "third" were copied, in that order
      When the user holds Ctrl+V until the picker appears, taps Down and taps Delete
      And lets go of Ctrl
      Then the picker closes and "first" is pasted
  how
    decide after Delete the choice stays in the same place, so the next older item is chosen; at the end of the list, the item above it
      because nothing was chosen after a delete (#246), then the one after the next (#439)
    decide Delete also leaves the actions and chooses that item's Paste
      because the actions on screen belonged to the deleted item
    decide letting go of Ctrl after a Delete pastes the item now chosen, even when it is the newest
      because "unknown, confirm with the owner: keep this? It pastes from the history rather than the untouched clipboard"
    decide deleting the last item closes the picker and pastes nothing
      because there is nothing left to choose
    decide a deleted pinned item is also deleted from the computer, and a deleted item is gone from a remembered history
      because "unknown, confirm with the owner: so a deleted item never comes back after a restart?"
    decide deleting does not clear the clipboard, even when the deleted item is what it holds
      because "unknown, confirm with the owner: should deleting the newest item also clear the clipboard?"
    decide there is no undo, and no key to delete everything; Clear history in the tray menu does that
      because "unknown, confirm with the owner": #425 asked for a long press of Delete to clear all
  copy
    footer.remove   "Remove"   next to the "Del" key in the picker's footer
  uses      GestureRules
  sources
    GitHub issues #246, #425, #439
    README.md, "Delete removes an item"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool: Delete on the second of three items, the "N items" count before and after, what Ctrl pastes after
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, DeleteSelected
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, ProcessAsync for DeleteItem
```
