# The picker while searching

What the picker looks like once S has started a search: a search box under
the title, the results in the history pane, and a footer for searching.

```ail
node SearchBar : view   level: experience
  why   The user must see at once that typing now goes to the picker, what the
        query found, and which keys still work.   <- SearchHistory
  what
    Scenario: the search begins
      When the user presses S during the gesture
      Then a search box appears under the title, with a magnifier and the placeholder "Search everything you copied"
      And the footer changes to "Enter Paste · ↑↓ Move · ←→ Actions · Esc Cancel"
    Scenario: the results
      When the query matches one item
      Then the history pane lists only it, chosen, and its count reads "1 item"
      And the preview and the actions beside it are those of the chosen result
    Scenario: nothing matches
      When the query matches nothing
      Then the history pane shows "No matches" and "Nothing you copied contains “…”." in place of the list, without the "History" heading or the count
      And the actions pane keeps its place, so the picker keeps its width
      And the preview above "Actions" is an empty card, grey shading at its bottom, with a small text icon
  how
    decide the search box sits under the title, not as a row of the list
      because PR #635: "There is no search row in the list"
      rejected a "false top" row of the list, which #458 first proposed, because "unknown, confirm with the owner"
    decide the actions pane stays while nothing matches
      because the picker keeps its width (PickerViewModel, ShowsActions)
    decide the empty preview card while nothing matches
      because "unknown, confirm with the owner: keep this? It looks like a leftover of the last preview"
    decide the footer while searching has no V, 1–9, Space, S or Del
      because those keys type into the search box now
  look
    the picker's usual header ("Shapeshifter" with its icon, centred), then the search bar: a full-width rounded field with a magnifier glyph on the left
    below it, the history pane ("History", the count of results) and, to its right, the preview and "Actions"
    the footer is one row of key caps, each followed by its word, centred
    while nothing matches, the picker is shorter: the history pane holds only the two lines of the empty state, the title bold
  copy
    search.placeholder      "Search everything you copied"
    search.nomatches        "No matches"
    search.nomatches.text   "Nothing you copied contains “{query}”."
    footer.paste            "Paste"     next to "Enter"
    footer.move             "Move"      next to "↑↓"
    footer.actions          "Actions"   next to "←→"
    footer.cancel           "Cancel"    next to "Esc"
    count                   "{count} items", "1 item"
  sources
    driven on Linux (Xvfb, openbox) 2026-11-20, SHAPESHIFTER_LOOK=linux: screenshots of the empty search, "CAFE" (1 item), "CAFE zzz" (No matches), the actions chosen with Right
    issue #458, comment of 2018-05-13 (the "false top" of the list); PR #635
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, the search bar, the search box and the footer shown while searching
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, HasNoMatches, NoMatchesText, ShowsActions
    src/Shapeshifter.Core/Localization/Strings.resx, PickerSearchPlaceholder, PickerSearchNoMatches, PickerSearchNoMatchesText, PickerHintPaste
```
