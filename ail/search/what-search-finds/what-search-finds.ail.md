# What a search finds

Which items a query finds: every word of it must occur somewhere in the item,
in its content or in where it was copied from.

```ail
node WhatSearchFinds : flow   level: experience
  why   People remember a word from what they copied, or where they copied it,
        rarely how it was spelled or what it looked like.   <- SearchHistory
  what
    Scenario: every word must occur, in any order, ignoring case and accents
      Given "Crème brûlée" and then "crème fraîche" were copied
      When the user searches for "BRULEE creme" and taps Enter
      Then "Crème brûlée" is pasted
    Scenario: a search that finds nothing says so
      Given "walnut" was copied
      When the user searches for "walnut pecan"
      Then the picker shows "No matches" and "Nothing you copied contains “walnut pecan”."
      When the user taps Enter
      Then nothing happens: the picker stays open and keeps the focus
    Scenario: the whole text is searched, not only its first line
      Given a shopping list was copied whose last line says "saffron"
      When the user searches for "saffron" and taps Enter
      Then the whole list is pasted, every line of it
    Scenario: copied HTML is searched by its words, not its markup
      Given a web page fragment "Hello tangerine" was copied, with a class name "marker" in its markup only
      When the user searches for "marker"
      Then the picker shows "No matches"
      When the user searches for "hello tangerine" instead and taps Enter
      Then the fragment is back on the clipboard, as HTML
    Scenario: a copied file is found by its path and by the word file
      Given a file "report.txt" was copied in another app
      When the user searches for "file report" and taps Enter
      Then that file is back on the clipboard
    Scenario: a copied picture is found by the word image
      Given a picture and then a caption were copied in a window titled "album"
      When the user searches for "image album"
      Then only the picture is left, titled "Image · 24 × 24"
    Scenario: an item is found by the window it was copied from
      Given "sunny weather" was copied in a window titled "notes"
      When the user searches for "notes" and taps Enter
      Then "sunny weather" is pasted
  how
    decide a query is split into words; an item matches when it contains every word, anywhere, in any order
      because "unknown, confirm with the owner: why every word, rather than any word as the 'any match' in #458 suggested"
    decide case and accents are ignored: "cafe" finds "Café"
      because "unknown, confirm with the owner"
    decide the results keep the order of the history, and the first is chosen
      because "unknown, confirm with the owner"
    decide an item is searched by its whole text, the words of copied HTML when there is no plain text, the paths of copied files, its kind and the app and window it came from
      because these are what #458's pull request set out to search (PR #635)
    decide the kind is searched in English only: "image", "file" or "files"
      because "unknown, confirm with the owner: keep this in other languages?"
    decide only the first 200,000 characters of a very long text are searched
      because searching must stay instant (HistorySearch, MaxContentLength)
  copy
    search.nomatches        "No matches"
    search.nomatches.text   "Nothing you copied contains “{query}”."     the query with the spaces around it removed
  uses      SearchRules
  sources
    issue #458, comment of 2018-05-13: "a text filter field for 'any match' results"
    PR #635, "Searching the history"
    driven on Linux (Xvfb, openbox) 2026-11-20: copies from the target app and from xterm windows with xclip (text, text/html only, text/uri-list, image/png); "CAFE zzz" showed No matches; "xterm" found everything copied in xterm windows
    src/Shapeshifter.Core/Clipboard/HistorySearch.cs
    src/Shapeshifter.Core/Localization/Strings.resx, PickerSearchNoMatches, PickerSearchNoMatchesText
```
