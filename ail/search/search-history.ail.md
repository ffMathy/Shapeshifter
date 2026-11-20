# Searching the history

S, pressed during the gesture, turns the picker into a search: the user types
a few words, and Enter pastes the item found into the app they came from.

```ail
node SearchHistory : flow   level: experience
  why   An older copy is hard to find by moving through a long history one item
        at a time; people asked to search it, as other clipboard managers do (#458).   <- Shapeshifter
  what
    Scenario: S starts a search, even before the history appears
      Given "pear" was copied
      And a text box in another app has the focus
      When the user holds Ctrl and V, taps S at once and lets go of V and Ctrl
      Then the picker appears with an empty search box showing "Search everything you copied"
      And the picker has the keyboard focus
      And no "s" reaches the text box
    Scenario: typing narrows the history, and Enter pastes into the app the user came from
      Given "plum", "fig" and "lime" were copied
      When the user starts a search and types "fig"
      Then nothing reaches the text box while typing
      When the user taps Enter
      Then the picker closes, the other app has the focus again, and "fig" is pasted, once
    Scenario: the arrows move through the results, wrapping around
      Given "kiwi red", "kiwi green" and "kiwi blue" were copied, in that order
      When the user searches for "kiwi", taps Up and taps Enter
      Then "kiwi red", the last result, is pasted
    Scenario: Right at the end of the query opens the actions
      Given "memo" was copied
      When the user searches for "memo", taps Right, Down and Enter
      Then the second action, "Paste as file", runs: the clipboard holds a .txt file containing "memo"
    Scenario: Right inside the query moves the caret instead
      Given "note" was copied
      When the user searches for "note", taps Left, then Right, then Enter
      Then "note" is pasted as text
    Scenario: Escape gives the focus back without pasting
      Given "first" and "second" were copied
      When the user starts a search, types "first" and taps Escape
      Then the picker closes, nothing is pasted, and what the user types next reaches the other app
    Scenario: clicking another window cancels the search
      When the user starts a search, types "apricot" and clicks the text box of the other app
      Then the picker closes, nothing is pasted, and the clicked window keeps the focus
    Scenario: Ctrl+V in the search box pastes nothing on Linux
      Given "clip" was copied
      When the user starts a search and presses Ctrl+V
      Then the search box stays empty and still shows "Search everything you copied"
  how
    decide S starts the search, whether the picker is up yet or not, with Ctrl held or after Space kept it open
      because the search is part of the gesture, reached with the keys already under the hand (PR #635)
      because "unknown, confirm with the owner: why S, rather than typing any letter"
    decide the picker takes the keyboard focus while searching, and only then
      because typing a query needs a real text box (every keyboard layout, dead keys, input methods), which the keyboard hook cannot provide (docs/architecture.md)
    decide Enter and Escape first give the focus back to the window the user came from
      because only then does the paste land in that window; from the wrong thread Windows refused at times and the paste went nowhere (commits 0eb19c7, f16712c)
    decide going to another window cancels the search and leaves the focus there
      because the user went there on purpose (PickerSearch, Deactivated)
    decide Right opens the actions only once the caret is at the end of the query
      because Right must still move the caret while the query is being edited (PickerWindow)
    decide Left goes back from the actions to the results and keeps the action chosen
      because "unknown, confirm with the owner: keep this?"
    decide on Linux, Ctrl+V does not reach the search box, although the pull request says it pastes there
      because "unknown, confirm with the owner: keep this?" (Shapeshifter's grab of Ctrl+V stays on while searching)
    decide the search starts on the first result, not on the item chosen before S
      because "unknown, confirm with the owner: keep this?"
  uses      SearchRules, SearchFocus
  children  WhatSearchFinds, SearchBar, SearchRules, SearchFocus
  sources
    issue #458 "Searching item history in the overlay"; PR #635 "Search the history with S …"
    commits f709ea8, 0eb19c7, f16712c
    docs/architecture.md, "Searching the history"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool: S before the show delay, after Space, with the picker up; typing, Up wrapping, Right/Left, Enter, Escape, a click on the other app, Ctrl+V
    tests/Shapeshifter.EndToEnd.Tests/PasteGestureEndToEndTests.cs, the two search tests
    src/Shapeshifter.Core/Input/PickerSearch.cs; src/Shapeshifter.Desktop/Views/PickerWindow.axaml.cs, OnKeyDownTunnel
```
