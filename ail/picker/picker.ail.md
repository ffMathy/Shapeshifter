# The picker

The window the history appears in while the user holds Ctrl+V: what was
copied, where it came from and when, a larger preview of the chosen item, what
pasting it will do, and the keys. Its layout is shared by every look; each look
gives it its own material, sizes, colours and type.

```
picker.ail.md                                        Picker (this file)
history-row/history-row.ail.md                       HistoryRow: one copied item in the list
history-row/row-content/row-content.ail.md           RowContent: a row's title, tile and second line
history-row/relative-time/relative-time.ail.md       RelativeTime: "Just now", "5 min ago"…
preview-card/preview-card.ail.md                     PreviewCard: the chosen item, larger
preview-card/preview-rules/preview-rules.ail.md      PreviewRules: what the card shows for each kind of item
actions-list/actions-list.ail.md                     ActionsList: how the actions look
picker-footer/picker-footer.ail.md                   PickerFooter: the key hints
empty-picker/empty-picker.ail.md                     EmptyPicker: nothing copied yet
open-picker/open-picker.ail.md                       OpenPicker: where it opens, never taking the focus
open-picker/placement-rules/placement-rules.ail.md   PlacementRules: the arithmetic
looks/…                                              Looks: Mica Sheets, Refraction, Boxed Sheet, accents, material
languages/…                                          Languages: 21 translations, right to left, the override
```

```ail
node Picker : view   level: experience
  why   The user chooses by eye while their hands stay on the keys, so each item
        must be recognisable at a glance and the chosen one shown in full before
        it is pasted (#54, #296, #341, #444).   <- Shapeshifter
  what
    a header, then the history on one side and the chosen item's preview above its actions on the other, then the key hints
    the history lists every item, newest first with pinned items on top, as rows; the chosen row is marked
    the preview and the actions always follow the chosen row
    the list shows as many whole rows as fit in about three fifths of the screen's height; the rest scroll into view as the choice moves
    Scenario: the parts of the picker
      Given "alpha", "bravo" and "dotnet run --project src/App" were copied, in that order
      When the user holds Ctrl+V until the picker appears
      Then the header says "Shapeshifter"
      And the history is titled "History" with "3 items" beside it
      And the row "dotnet run --project src/App" is chosen, its preview shows the same text and its actions are listed under "Actions"
      And the footer shows the keys
    Scenario: one item is counted as such
      Given only "solo" was copied
      When the user holds Ctrl+V until the picker appears
      Then the count says "1 item"
    Scenario: a long history scrolls
      Given eight items were copied, on a screen 800 pixels high
      When the user holds Ctrl+V until the picker appears
      Then seven whole rows show and the count says "8 items"
      When the user moves the choice to the eighth
      Then the list scrolls so the chosen row shows
  how
    decide one layout for every look, with each look styling its parts
      because Shapeshifter should match the design language of each operating system without three copies of the picker (commit "Prepare a look per operating system")
    decide the history may use 62 % of the screen's height, in whole rows only
      because the last visible row is never cut in half (PickerPresenter)
      because "unknown, confirm with the owner: why 62 %"
    decide the counts are "1 item" and "N items" in every language, with the number written the local way
      because the translations carry a singular and a plural form (#49, PR #633)
  look
    window      no title bar or frame, never in the taskbar, always on top, sized to its content; the panel fills it on a desktop without a compositor (square, opaque, no shadow), else floats with a shadow
    header      the app's icon and the picker's title; in some looks the count and the paste hint sit here too
    body        two columns: the history list on the start side, a column on the end side with the preview card above the actions list
    history     a list without a visible scroll bar; rows of a fixed height per look
    footer      the key hints, wrapping onto more lines rather than widening the picker
    widths      Linux 756, Windows and macOS similar; the picker does not grow with long texts, they are cut with "…"
  copy
    title.linux      "Shapeshifter"
    title.windows    "Clipboard history"
    title.macos      "Clipboard"
    count.one        "1 item"
    count.other      "{count} items"
    heading.history  "History"      Linux look only, never translated
  children  HistoryRow, PreviewCard, ActionsList, PickerFooter, EmptyPicker, OpenPicker, Looks, Languages
  sources
    issues #54, #144, #296, #341, #435, #444; PRs #618, #623, #634, #646
    docs/architecture.md, "The picker" and "Looks"
    driven on Linux (Xvfb, openbox) 2026-11-20: 1 to 18 items, text, code, colour, image, one file, two files, a linked image; 8 items show 7 rows on 1280 × 800
    assets/screenshots/linux.PNG, windows.PNG, macos.PNG (generated by tools/Shapeshifter.ScreenshotGenerator)
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs
    src/Shapeshifter.Desktop/Services/PickerPresenter.cs, SetMaxListHeight and HeightFraction
```
