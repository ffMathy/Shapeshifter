# A row of the history

One copied item in the picker's list: a tile that shows what kind of thing it
is, the content, where it came from, when, and a pin when it is pinned.

```ail
node HistoryRow : view   level: experience
  why   Copies from the same place look alike in words, so each row shows the
        thing itself where it can (a thumbnail, a colour, a file's icon), where it
        came from and when (#296, #299, #435, #444, PR #618).   <- Picker
  what
    a row has, from the start side: a tile, the content over a second line, and at the end side a pin (pinned items only) and when it was copied
    the tile is a thumbnail of a copied image, the system icon of a single file, up to four icons of several files (one per kind of file), the colour of a copied colour code, or else a line icon for the kind of item
    the content is one line of the text (two in the Windows look), cut with "…"; for an image its size, for files their names
    the second line names the window it was copied from, with that app's icon; when that is unknown, the folder of copied files, or a description of the text
    the time is in words and is refreshed each time the picker opens; copying the same thing again refreshes it, pinned items keep theirs
    Scenario: a text copied in another app
      Given "focus 7svfio" was copied in the window "Shapeshifter E2E Target"
      When the user holds Ctrl+V until the picker appears
      Then its row shows a page icon, "focus 7svfio" and, below it, that window's icon and "Shapeshifter E2E Target", and "Just now" at the end
    Scenario: a text from an unknown window describes itself
      Given "dotnet run --project src/App" was copied where no window could be named
      Then its second line shows a globe and "28 characters"
      And a text of four lines that ends with a line break says "5 lines"
    Scenario: an image
      Given a 320 × 200 image was copied
      Then its tile is the image itself and its row says "Image · 320 × 200", with no second line when no window was named
    Scenario: files
      Given "notes.txt" alone was copied from the folder "files"
      Then its tile is the file's system icon, its row says "notes.txt" and, below, a folder icon and "files"
      Given "notes.txt" and "report.pdf" were copied together
      Then its tile shows the two files' icons side by side and its row says "2 items · notes.txt, report.pdf"
    Scenario: a colour code
      Given "#2c5497" was copied
      Then its tile is filled with that blue and the row says "#2c5497"
    Scenario: a pinned item
      Given "public void Main() {" was copied and pinned five minutes ago
      Then its row sits at the top with a pin in the accent colour before "5 min ago"
    Scenario: the chosen row
      Given the picker is up with the second row chosen
      Then that row is tinted with the accent colour and, while the history has the keyboard, ringed in it; its tile turns white and its icon takes the accent
  how
    decide the second line falls back to the folder of files, then to a description of the text
      because a file manager's window is usually named after that folder anyway (PickerItemViewModel)
    decide a line break at the end of a text counts as one more line
      because "unknown, confirm with the owner: keep this?"
    decide the times are refreshed only when the picker opens, not while it is up
      because the picker reuses its rows each time it is shown (PR #623)
  look
    row         64 high in the Linux look, its tile 40 × 40 with rounded corners, then 12 of space, then the content; the end column holds the pin and the time
    title       one line, regular weight, the body text colour; medium weight on the chosen row
    second      smaller, dim text after a 12 × 12 app icon, or a globe or folder line icon; at most 250 wide in the Linux look
    time        small dim text; a 13 × 13 pin before it in the accent colour, darker on the chosen row
    chosen      Linux: accent tint and a 2-pixel accent ring inset by 2 with rounded corners; Windows: a subtle fill and a 3 × 16 accent pill at the start edge; macOS: a capsule filled with the accent, white text and a chevron toward the actions
    hairlines   Linux rows are split by hairlines inside a white card with rounded corners
  copy
    image.size     "Image · {width} × {height}"
    image          "Image"
    files          "{count} items · {names}"      the first six names, comma separated
    empty          "(empty)"
    characters     "{count} characters"
    lines          "{count} lines"
    formatted      "Formatted text"
    other          "Other data"
  children  RowContent, RelativeTime
  sources
    issues #296 thumbnails, #299 colour codes, #341 two lines of text, #435 pin, #444 times; PRs #618, #623
    driven on Linux (Xvfb, openbox) 2026-11-20: text from the target app and from xclip, code, a colour, an image, one file, two files, a pinned item after five minutes
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, the row template
    src/Shapeshifter.Desktop/ViewModels/PickerItemViewModel.cs
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml, MacOS.axaml, "history rows"
```
