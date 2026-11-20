# What the preview card shows

```ail
node PreviewRules : fn   level: system
  why   The card's content and caption must be decided from the item alone, the
        same in every look, and tested without a window.   <- PreviewCard
  what
    PreviewText(item: ClipboardItem) -> text   — "" for anything but text, and for a colour code
    LooksLikeCode(text: text or nothing) -> boolean
    PreviewCaption(item: ClipboardItem, title: text) -> text
    ColorHex(text: text) -> text
    ColorRgb(text: text) -> text
    type LinkedImage = { link: text or nothing, filePath: text or nothing, Location: text }
    LinkedImage.Find(text: text or nothing) -> LinkedImage or nothing   — link is an absolute web address
    A LinkedImage's Location is the link's host, or the file's folder (the file path itself when it has none).
    MorePreviewFiles(files: list of text) -> text   — "+{count − 6}" when more than six, else ""
    ShowsPreviewGlyph(item: ClipboardItem) -> boolean   — true when there is no preview text, no image, no colour and not files
    examples
      PreviewText(text "a\tb\r\nc") => "a    b\nc"; every line ending one line break, a tab four spaces
      PreviewText(text of 2500 characters) => its first 2000 characters + "…"
      PreviewText(text "#2c5497") => ""
      PreviewText(image) => ""
      LooksLikeCode("dotnet run --project src/App") => true; one line with a word starting with "-"
      LooksLikeCode("Use -f. It forces it") => false; one line with ". " is a sentence
      LooksLikeCode("hello world") => false
      LooksLikeCode("public void Main()\n{\n    Console.WriteLine(\"hi\");\n}\n") => true
      LooksLikeCode("a;\nb\nc") => true; at least one non-blank line in three ends in ; { } ) > or is indented
      LooksLikeCode("a;\nb\nc\nd") => false
      LooksLikeCode("Dear Ann,\nThanks for the call.\nBest, Bo") => false
      PreviewCaption(text "dotnet run --project src/App") => "28 characters"
      PreviewCaption(text "line one\nline two") => "2 lines · 17 characters"
      PreviewCaption(text "x" with rich text) => "Formatted text · 1 characters"
      PreviewCaption(image 320 × 200, title "Image · 320 × 200") => "Image · 320 × 200"
      PreviewCaption(file "/tmp/files/notes.txt") => "/tmp/files", after a folder icon
      PreviewCaption(files notes.txt and report.pdf) => "2 files"
      PreviewCaption(text "#2c5497") => "rgb(44, 84, 151)";  ColorHex("#2c5497") => "#2C5497"
      PreviewCaption(text "#2c549780") => "rgba(44, 84, 151, 0.5)";  ColorHex("#2c549780") => "#2C549780"
      PreviewCaption(other data) => "Other data"
      LinkedImage.Find("https://i.imgur.com/cat.png") => link, Location "i.imgur.com", caption after a link icon
      LinkedImage.Find("https://example.com/page") => nothing; not an image
      LinkedImage.Find("see https://i.imgur.com/cat.png") => nothing; a link may not contain spaces
      LinkedImage.Find("\"/home/u/My Pictures/cat.JPG\"") => file "/home/u/My Pictures/cat.JPG", Location "/home/u/My Pictures"
      LinkedImage.Find("file:///home/u/a.webp") => file "/home/u/a.webp"
      LinkedImage.Find("C:\\Pics\\cat.png") => file; LinkedImage.Find("Pics/cat.png") => nothing; only full paths
      LinkedImage.Find("#ff0000") => nothing; a colour code is never a linked image
      MorePreviewFiles(eight files) => "+2"
  how
    decide image formats are recognised by extension: .png .jpg .jpeg .gif .bmp .webp, any case
      because only those can be decoded for the card (LinkParser)
    decide a linked image is loaded only for the chosen row, at most 10 MB, the last 16 kept, and drawn at most 360 pixels high
      because the picker contacts a site only for the link the user looks at, and must stay light (#21, PR #634)
    decide a copied image is decoded 360 pixels high for the card, and 128 high for the rows' tiles
      because both stay sharp at 200 % display scaling (PickerPresenter)
    decide "1 characters" is not made singular
      because "unknown, confirm with the owner: keep this?"
  sources
    src/Shapeshifter.Desktop/ViewModels/PickerItemViewModel.cs, LooksLikeCode, CreatePreviewText, CreatePreviewCaption
    src/Shapeshifter.Core/Previews/LinkedImage.cs and LinkedImageLoader.cs; src/Shapeshifter.Core/Text/LinkParser.cs
    src/Shapeshifter.Desktop/Services/PickerPresenter.cs, ThumbnailHeight, PreviewImageHeight
    issue #21; PRs #634, #642
```
