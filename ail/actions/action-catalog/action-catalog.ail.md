# The action catalog

Every action, the order they are listed in, which items each applies to, and
what each one does.

```ail
node ActionCatalog : fn   level: system
  why   The list the user moves through by position must be the same on every
        platform and for every build, so which actions apply and their order
        are exact rules, testable without a desktop.   <- Actions
  what
    exports
      interface IClipboardAction { Id: string; Order: int; GetTitle(item) => string; GetDescription(item) => string; CanExecute(item) => bool; ExecuteAsync(item, context: ActionContext) => Task }
      ActionCatalog(actions: IClipboardAction[])   All sorted by Order;  GetAvailable(item) => All where CanExecute(item)
      ActionCatalog.CreateDefault()
      ActionContext.WriteAndPasteAsync(snapshot)   writes the clipboard, waits PasteSettleDelay = 40 ms, then simulates Ctrl+V (Cmd+V on macOS)
      ActionContext.CopyNewContentAsync(snapshot)  writes the clipboard and adds the snapshot to the history as its newest item; no paste
    catalog   id                     order  offered when
              paste                  0      always
              paste-as-file          10     an image with bytes, or Text kind with non-empty text
              paste-plain-text       20     Text kind with a rich format (HTML or RTF), and either non-empty text or an HTML format
              upload-image           30     an image with bytes (UploadImage)
              copy-as-zip            75     File or FileCollection kind
              copy-image-from-link   100    Text kind whose text holds a link to an image
              open-links             200    Text kind whose text holds at least one link; opens at most 10
              toggle-pin             255    always; titled "Unpin from clipboard" when the item is pinned
    examples
      CreateDefault().All.Select(a => a.Id) => [paste, paste-as-file, paste-plain-text, upload-image, copy-as-zip, copy-image-from-link, open-links, toggle-pin]
      GetAvailable(text "hello") => [paste, paste-as-file, toggle-pin]
      GetAvailable(text "x" + raw "text/html") => [paste, paste-as-file, paste-plain-text, toggle-pin]
      GetAvailable(raw "text/html" only, no plain text) => [paste, paste-plain-text, toggle-pin]
      GetAvailable(raw "Rich Text Format" only, no plain text) => [paste, toggle-pin]
      GetAvailable(PNG image) => [paste, paste-as-file, upload-image, toggle-pin]
      GetAvailable(image + one temporary image file, as Firefox copies) => [paste, paste-as-file, upload-image, toggle-pin]
      GetAvailable(text "chart" + image, as office suites copy) => [paste, paste-as-file, toggle-pin]; paste-as-file writes the text
      GetAvailable(files ["/a/x.txt"]) => [paste, copy-as-zip, toggle-pin]
      GetAvailable(raw "application/x-weird" only) => [paste, toggle-pin]
      GetAvailable(text "see https://example.com/a.png and https://example.org") => [paste, paste-as-file, copy-image-from-link, open-links, toggle-pin]
      open-links.GetTitle(text "https://example.org") => "Open link";  with two links => "Open links"
      paste-as-file.GetDescription(JPEG image) => "Saves this image to a .jpg file and pastes the file."
      paste: ExecuteAsync(item) => WriteAndPasteAsync(item.Snapshot), then History.MoveToTop(item.Id) (no move for a pinned item)
      paste-plain-text: no plain text + HTML "<span>Hello <b>bold</b> <a href=\"x\">world</a></span>" => pastes { Text: "Hello bold world" }
      paste-as-file: text item captured 2026-11-20 12:57:21 local => <temp>/Text files/<id N>/Text 2026-11-20 12.57.21.txt, UTF-8 with a byte order mark, pasted as { Files: [that path] }
      paste-as-file: JPEG image => <temp>/Image files/<id N>/Image <time>.jpg
      toggle-pin, unpinned => PinnedItems.SaveAsync(item), then History.SetPinned(id, true)
      toggle-pin, pinned => History.SetPinned(id, false), then PinnedItems.DeleteAsync(id)
      copy-as-zip(["/a/x.txt", "/a/y.txt"]) => <temp>/Compressed folders/<8 hex>/a.zip, copied, not pasted;  one file "/a/b.txt" => b.zip;  parents differ => Archive.zip
  how
    decide the gesture refers to an action by its index in the item's list, computed when the item is added and cached per item
      because the list must be known on the keyboard hook's thread without work; titles are made when shown, so Pin/Unpin follows the item
    decide an action is recognised by its Id, never by its title
      because titles are translated into 21 languages (docs/architecture.md, "Languages")
    decide temp is %TEMP%\Shapeshifter (Windows, macOS), or on Linux $XDG_RUNTIME_DIR/shapeshifter, else <data>/temp; deleted when the app starts
      because /tmp on Linux is shared with other users, and the files hold copied content
    decide a failing action is logged and the gesture still ends; the user sees nothing
      because "unknown, confirm with the owner: should a failed action tell the user?"
  sources
    src/Shapeshifter.Core/Actions/*.cs; src/Shapeshifter.Core/Clipboard/ImageFormat.cs; src/Shapeshifter.Core/Clipboard/ClipboardSnapshot.cs, Kind
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, CreateSession, PerformAsync and ProcessEffectsAsync
    src/Shapeshifter.Core/Settings/AppPaths.cs, TempDirectory
    driven on Linux 2026-11-20: the actions of plain text, HTML text, a PNG, files, other data and links; the Text and Image files written on disk
```
