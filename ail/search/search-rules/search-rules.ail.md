# Search rules

The search as pure code: matching (HistorySearch) and the state machine that
the focused picker drives (PickerSearch). Neither touches the operating system.

```ail
node SearchRules : fn   level: system
  why   What a query finds and what each key does must be exact and testable
        without a desktop, like the gesture's own rules.   <- SearchHistory
  what
    HistorySearch.Normalize(text: text) -> text — lower case (invariant), accents (non-spacing marks) removed, NFC
    HistorySearch.ParseQuery(query: text or nothing) -> list of text — normalized words, split on white space, duplicates removed; blank => []
    HistorySearch.Matches(searchText: text, terms: list of text) -> boolean — every term is an ordinal substring; [] matches everything
    HistorySearch.CreateContentText(item: ClipboardItem) -> text — preview, kind word, text (else HTML as plain text), file paths; normalized, joined by "\n"
    HistorySearch.CreateSearchText(contentText: text, item: ClipboardItem) -> text — adds "\n" + normalized application name and window title, when known
    HistorySearch.MaxContentLength: integer = 200000
    type SearchableItem = { Item: GestureItem, SearchText: text }
    PickerSearch(items: list of SearchableItem) -> PickerSearch
    PickerSearch.Handle(input: PickerSearchInput) -> GestureResult
    PickerSearch.Query: text
    PickerSearch.HasEnded: boolean
    PickerSearch.Selection: PickerSelection
    type PickerSearchKey = "QueryChanged" | "Up" | "Down" | "Left" | "Right" | "Enter" | "Escape" | "Deactivated"
    examples
      Normalize("Crème BRÛLÉE") => "creme brulee"
      ParseQuery("  Café  cafe ") => ["cafe"];  ParseQuery("   ") => []
      Matches("apple one", ParseQuery("ONE app")) => true;  Matches("apple one", ParseQuery("apple two")) => false
      kind word: Image => "image", File => "file", FileCollection => "files", Text and others => ""
      Matches(CreateContentText(HTML only "<p>Hi <span class=\"k\">there</span></p>"), ParseQuery("hi there")) => true; with ParseQuery("span") => false
      Matches(CreateContentText(text "plain" and HTML "<b>bold</b>"), ParseQuery("bold")) => false: the HTML is searched only when there is no text
      CreateContentText(a 300,000-character text) => its preview and its first 200,000 characters only
      PickerSearch(3 items).Selection => items [0, 1, 2], selected 0, pane Items, Query ""
      Typed("b") with 2 of 3 matching => [UpdatePicker(results in history order, selected 0, action 0, pane Items)]
      Typed(same query again) => passes through, no effects
      3 results, selected 0, Up => UpdatePicker(selected 2);  selected 2, Down => UpdatePicker(selected 0)
      0 results, Up or Down => swallowed, no effects;  0 results, Enter => swallowed, no effects, not ended
      Right => [UpdatePicker(pane Actions)]; Right again, or with 0 results, or on an item with no actions => passes through
      pane Actions, Down => UpdatePicker(action 1), wrapping over the item's actions; the item stays
      pane Items, Left => passes through (the search box moves its caret);  pane Actions, Left => UpdatePicker(pane Items), the action stays chosen
      pane Items, Down => the action goes back to 0
      selected 1, action 1, Enter => [RestoreFocus, HidePicker, PerformAction(result 1, action 1)]; HasEnded
      Escape => [RestoreFocus, HidePicker, GestureCancelled]
      Deactivated => [HidePicker, GestureCancelled], no RestoreFocus
      after it ended, any input => passes through, no effects
      gesture: S with items, picker hidden => [ShowPicker(sel), BeginSearch(sel)], suppressed; the gesture ends
      gesture: S with the picker up => [BeginSearch(sel)];  S with an empty history => swallowed, no effects
  how
    decide matching is an ordinal substring search over text normalized once
      because it is a plain, fast search over every item (HistorySearch)
    decide an item's content text is computed once and cached by its id; where it was copied from is added each time
      because content never changes, but copying it again updates where it came from (HistorySearch.CreateContentText)
    decide the search starts from the gesture's items in their order, with the first selected, whatever was chosen before S
      because "unknown, confirm with the owner"
    decide Enter with no results does nothing and the search goes on
      because "unknown, confirm with the owner"
    decide the picker reports only Up, Down, Left, Right (caret at the end, nothing selected), Enter and Escape, and only without modifiers or with Shift; every other key edits the query
      because the search box must get the typing (PickerWindow.OnKeyDownTunnel)
  verify
    property: Matches(Normalize(t), ParseQuery(q)) is unchanged by the case and the accents of q
    property: the results are always a sub-sequence of the items, in order
    property: after HasEnded, Handle returns no effects for any input
  sources
    src/Shapeshifter.Core/Clipboard/HistorySearch.cs
    src/Shapeshifter.Core/Input/PickerSearch.cs
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, OnNavigationKey (GestureKey.Search)
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, BeginSearchAsync (searchTexts cache)
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml.cs, OnKeyDownTunnel
    tests/Shapeshifter.Core.Tests, PickerSearch and HistorySearch tests (PR #635)
```
