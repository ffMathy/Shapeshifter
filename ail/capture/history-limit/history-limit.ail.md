# How much the history keeps

The history keeps the newest copies up to a number the user sets, "Items to
remember" (50 unless changed). Pinned items are kept on top, in addition.

```ail
node HistoryLimit : flow   level: experience
  why   A history that grows forever gets slow and long to look through, but
        a limit must never cost the user what they chose to keep.   <- Capture
  what
    Scenario: the history keeps the newest items, up to Items to remember
      Given "Items to remember" is set to 3 in the settings
      And "first", "second", "third" and "fourth" were copied, in that order
      When the user holds Ctrl+V and taps 4
      Then nothing is chosen and "fourth" is pasted as usual
      When the user holds Ctrl+V and taps 3
      Then "second" is pasted: "first" was dropped
    Scenario: a pinned item stays on top and keeps its place
      Given "filler" and then "pinned" were copied, and "pinned" was pinned
      And "newer" and then "newest" were copied after it
      When the user holds Ctrl+V
      Then the picker starts on "newest", and a quick release pastes it
      And Up from there chooses "pinned", above it
      When "pinned" is copied again
      Then it stays on top and the history below it starts "newest", "newer"
  how
    decide the limit counts only unpinned items; the oldest unpinned items go first
      because pinned items are what the user chose to keep ("Pinned items are kept in addition", the setting's description)
    decide 50 by default, from 1 to 1000
      because "unknown, confirm with the owner: why 50 and 1000; v6 removed its limit after #244"
    decide lowering the limit drops the extra oldest items at once
      because "unknown, confirm with the owner"
    decide pinned items sit on top in the order they were pinned, and copying one again does not move it
      because "unknown, confirm with the owner"
    decide the picker still starts on the newest unpinned item
      because a plain paste must still mean the latest copy (GestureRules)
  copy
    settings.items          "Items to remember"
    settings.items.detail   "Pinned items are kept in addition"
  sources
    GitHub issue #244 "Maximum number of items in clipboard could cause lost data?" (v6 removed its limit)
    driven on Linux (Xvfb, openbox, an SNI tray) 2026-11-20: "Items to remember" typed in the settings window; pinned with the picker's last action
    src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs, MaxUnpinnedItems, TrimLocked, Add, SetPinned
    src/Shapeshifter.Core/Settings/ShapeshifterSettings.cs, MaxHistoryItems and Normalize
    src/Shapeshifter.Desktop/App.axaml.cs, settings.Changed
```
