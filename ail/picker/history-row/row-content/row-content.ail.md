# What a row says

The title, tile and second line of a history row, worked out from the copied
item alone, so they are the same in every look and testable without a window.

```ail
node RowContent : fn   level: system
  why   The rows must describe every kind of copied thing the same way on every
        platform, in every language.   <- HistoryRow
  what
    type PickerItemLayout = "Text" | "Image" | "Files" | "File"
    Layout(item: ClipboardItem, thumbnail) -> PickerItemLayout — Image when the item is an image and its thumbnail decoded; Files for several files; File for one; Text otherwise
    Title(item: ClipboardItem) -> text
    SourceText(item: ClipboardItem) -> text
    ColorSwatch(item: ClipboardItem) -> Colour or nothing
    DescribeImage(png: bytes or nothing) -> text
    ReadPngSize(png: bytes or nothing) -> { width: integer, height: integer } or nothing
    DescribeFiles(files: list of text) -> text
    FilePreviewKeys(files: list of text) -> list of text — at most 4 icon keys, one per kind: "<folder>" for folders, the full path for .exe .lnk .ico .app .desktop .url, else the upper-cased extension
    examples
      DescribeImage(png of 320 × 200) => "Image · 320 × 200"
      DescribeImage(bytes that are not a PNG) => "Image"
      ReadPngSize(a PNG of 1920 × 1080) => { width: 1920, height: 1080 }, read big-endian from bytes 16 to 23 after the 8-byte signature
      DescribeFiles(["/d/Projects/", "/d/Notes.txt", "/d/Holiday.mp4", "/d/Sunset.png"]) => "4 items · Projects, Notes.txt, Holiday.mp4, Sunset.png"
      DescribeFiles(eight files a.txt … h.txt) => "8 items · a.txt, b.txt, c.txt, d.txt, e.txt, f.txt"
      Title(text "  hello \n\n  world  ") => "hello world"
      Title(text of 600 characters) => its first 512 characters, whitespace collapsed
      Title(text "   ") => "(empty)"
      Title(one file "/home/u/files/notes.txt") => "notes.txt"
      SourceText(item copied in a window titled "Notes — gedit") => "Notes — gedit"; the window title wins over the app name
      SourceText(text "abc", no window known) => "3 characters"
      SourceText(text "a\nb\n", no window known) => "3 lines"
      SourceText(text with rich text and no window known) => "Formatted text"
      SourceText(files ["/home/u/files/notes.txt"], no window known) => "files", with the folder's icon or else a folder line icon
      SourceText(files ["/notes.txt"], no window known) => "/"
      SourceText(image, no window known) => ""
      SourceText(other data, no window known) => "Other data"
      ColorSwatch(text "#2c5497") => rgb 44, 84, 151
      ColorSwatch(text " rgb(44, 84, 151); ") => rgb 44, 84, 151
      ColorSwatch(text "#2c5497 is blue") => nothing
      Layout(image whose PNG cannot be decoded) => Text, with the image line icon
  how
    decide the title of a text is the text with every run of whitespace and control characters made one space, trimmed, at most 512 characters
      because a row has one or two lines; the full text is in the preview (TextPreview)
    decide a colour is recognised only when the whole text is one: #rgb, #rgba, #rrggbb, #rrggbbaa, rgb() or rgba(), at most 64 characters
      because "#2c5497 is blue" is prose, not a colour (#299, ColorParser)
    decide the line count is the number of line breaks plus one
      because "unknown, confirm with the owner: a trailing line break counts as a line; keep this?"
    decide files with the same kind of icon share one place on the tile
      because four places show at most four kinds of file (PickerVisuals.GetIconKey)
  sources
    src/Shapeshifter.Desktop/ViewModels/PickerItemViewModel.cs, constructor, DescribeImage, DescribeFiles, CreateFallbackSubtitle, CountLines
    src/Shapeshifter.Core/Clipboard/ClipboardItem.cs, CreatePreviewText; src/Shapeshifter.Core/Text/TextPreview.cs
    src/Shapeshifter.Core/Text/ColorParser.cs; src/Shapeshifter.Desktop/Services/PickerVisuals.cs, GetIconKey
    src/Shapeshifter.Core/Platform/IClipboardSourceLocator.cs, DisplayText
    driven on Linux 2026-11-20: "28 characters", "5 lines" for four lines and a final line break, "files", "2 items · notes.txt, report.pdf"
```
