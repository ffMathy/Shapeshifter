# Remembering the history after a restart

A setting keeps the whole history, not just the pinned items, across
restarts of Shapeshifter and of the computer. It is off unless turned on.

```ail
node RememberHistory : flow   level: experience
  why   People asked for the history to survive a restart, as a backup of the
        clipboard that outlives a reboot (#459, #577); others copy passwords
        and want nothing written to disk, so it is a choice.   <- PinItems
  what
    Scenario: with the setting on, the whole history comes back in its order
      Given "Remember history after restart" was turned on in the settings
      And "apple", "banana" and "cherry" were copied, in that order
      When the user quits Shapeshifter from its tray menu, closes the app they copied from, and starts Shapeshifter again
      And holds Ctrl+V until the picker appears
      Then the history lists "cherry", "banana", "apple", in that order
      And tapping 3 and letting go of Ctrl pastes "apple"
    Scenario: with the setting off, only pinned items come back
      Given the setting was never turned on, and "lost" and then "current" were copied
      When the user quits Shapeshifter and starts it again
      Then the history shows "current", which is still on the clipboard, but not "lost"
      And no saved history is in Shapeshifter's data folder
    Scenario: turning the setting off deletes the saved history
      Given the setting was turned on and "saved" was copied
      Then a "History" folder with an "order.json" file is in Shapeshifter's data folder
      When the user turns the setting off
      Then that folder is gone
      When "after" is copied and the user quits and starts Shapeshifter again
      Then the history does not show "saved"
  how
    decide off by default
      because by default only pinned items are ever written to disk (commit b3f57d8)
    decide the history is saved as it changes, in the background, not when Shapeshifter quits
      because it must survive a reboot, not only a clean quit (#459); a burst of copies is saved once
    decide turning it off deletes what was saved, at once
      because "unknown, confirm with the owner: so nothing of the history is left on disk?"
    decide on start, the saved items go below the pinned ones and the clipboard is read after them, so what it holds lands on top
      because the newest copy is still what was copied last (commit b3f57d8)
    decide restored items keep when they were copied, and the "Items to remember" limit still applies to them
      because "unknown, confirm with the owner"
    decide the saved history sits next to the pinned items, in Shapeshifter's data folder
      because "unknown, confirm with the owner: is it worth encrypting what is saved?"
  copy
    setting.title         "Remember history after restart"
    setting.description   "Keeps the history on this computer's disk, not just pinned items"
    tray.settings         "Settings…"            the tray menu item that opens the settings; a click on the tray icon does too
  uses      SavedItems
  sources
    GitHub issues #459 "Save items to disk automatically", #577 "Data Persistence"
    commit b3f57d8 "Optionally remember the whole history after a restart (#459, #577)"; docs/architecture.md, "Capturing copies"
    driven on Linux (Xvfb, openbox, an xfce4-panel tray) 2026-11-20: the switch in the settings window, the History folder appearing and disappearing, quitting from the tray and starting again
    src/Shapeshifter.Core/Persistence/HistoryPersistence.cs; src/Shapeshifter.Core/Settings/ShapeshifterSettings.cs, RememberHistory
    src/Shapeshifter.Desktop/App.axaml.cs, StartAsync and the settings Changed handler
```
