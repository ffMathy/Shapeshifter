# The preview card

The chosen item, larger, at the top of the column beside the history, above
its actions.

```ail
node PreviewCard : view   level: experience
  why   One or two lines of a row are often not enough to tell what will be
        pasted; the chosen item should be seen in full first (#54, #341), and a
        link or path to an image should show the image (#21).   <- Picker
  what
    a card that grows with its content up to a maximum height, with a strip along its bottom that names what the item is next to a small line icon
    text shows its first lines with its line breaks; text that reads as code or a command is in a monospaced font
    an image fills the card from edge to edge
    a colour code shows a large swatch next to its code in capitals
    a single file shows its icon and its name; several files list the first six with their icons, then "+N" for the rest
    a text that is only a link to an image, or the path of an image file, shows that image filling the card, darkened towards the bottom, with a text icon and the link written over it in white; the row keeps its page icon
    anything else shows a large line icon of its kind
    the card follows the chosen row, and is left out when the picker would not fit on the screen with it
    Scenario: code
      Given "dotnet run --project src/App" was copied
      When the user holds Ctrl+V until the picker appears
      Then the card shows "dotnet run --project src/App" in a monospaced font, and the strip says "28 characters"
    Scenario: text of several lines
      Given "line one of three", "line two of three" and "line three of three" were copied as one text on three lines
      Then the card shows the three lines as three lines, and the strip says "3 lines · 55 characters"
    Scenario: a colour
      Given "#2c5497" was copied
      When the user moves the choice to it
      Then the card shows a blue square and "#2C5497", and the strip says "rgb(44, 84, 151)"
    Scenario: an image
      Given a 320 × 200 image was copied
      When the user moves the choice to it
      Then the image fills the card and the strip says "Image · 320 × 200"
    Scenario: files
      Given "notes.txt" and "report.pdf" were copied together
      Then the card lists "notes.txt" and "report.pdf" with their icons, and the strip says "2 files"
      Given only "notes.txt" was copied, from the folder "/tmp/files"
      Then the card shows its icon and "notes.txt", and the strip shows a folder icon and "/tmp/files"
    Scenario: the path of an image
      Given the text "/tmp/pictures/sunset.png" was copied and that file exists
      When the user moves the choice to it
      Then the card shows that image, with a text icon and the path over its darkened bottom, and the strip shows a folder icon and "/tmp/pictures"
    Scenario: a search that finds nothing
      Given the user searches for "zzqq" and nothing matches
      Then the card stays, empty: a dark band over its lower half with an empty text icon, and an empty strip
  how
    decide the card stays beside an empty search, in the shape of a linked image
      because "unknown, confirm with the owner: keep this? the card has no item to show"
    decide the card grows with its content up to a maximum, so a short text gets a short card
      because the picker should not be taller than it needs to be (PR #634)
    decide only the chosen item's image is decoded at preview size, and only the chosen link's image is fetched
      because the history must never hold more than one large image, and the picker contacts a site only for the link the user looks at (PR #634, #21)
    decide a copied image fills the card edge to edge, as a linked image does
      because the two looked different for no reason (PR #642)
    decide the strip reuses the rows' wording ("Formatted text", counts, "{count} files")
      because the card then needs no translations of its own (PR #634)
    decide the colours over a linked image are the same in every look
      because they are drawn over a picture, not over the look's surface (PickerWindow.axaml)
  look
    card       Linux: white, rounded 12, a hairline border, 30 below the column's top so it is level with the history's card, 12 above the actions; Windows: the tile fill and stroke, rounded 8; macOS: near-white, rounded 12, at the top of the glass sidebar
    content    14 by 12 of padding; at most 196 high (Linux) or 212 (Windows, macOS), clipped
    text       13 on a 19 line height, at most 9 lines, wrapped and cut with "…"; code 12 on 18 in the look's monospaced font, at most 10 lines
    colour     a 56 × 56 swatch with a hairline border, then the code in the monospaced font, 20, semibold
    file       a 40 × 40 icon, then the name 14 semibold on at most 2 lines; files: rows 26 high with an 18 icon and the name 13; "+N" 12 dim
    image      fills the card's width, at least 168 high; a linked image is darkened from transparent at 30 % of its height to black at 72 % at the bottom; the link in white 13 on at most 2 lines after a 28 × 28 white-on-glass text icon
    strip      a slightly darker band below a hairline; a 12 line icon and the caption, 11, dim, cut with "…"
  copy
    caption.files   "{count} files"
    caption.count   "{lines} lines · {count} characters"     the line count only for two lines or more
    caption.rich    "Formatted text · {size}"
    more.files      "+{count}"
  children  PreviewRules
  sources
    issues #21, #54, #341; PRs #634, #642
    driven on Linux (Xvfb, openbox) 2026-11-20: code, three lines, a colour, an image, one file, two files, the path of a PNG
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, "previewPane"
    src/Shapeshifter.Desktop/Services/PickerPresenter.cs, UpdatePreviewImage and LoadLinkedImageAsync
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml, MacOS.axaml, "previewPane" to "previewCaption"
```
