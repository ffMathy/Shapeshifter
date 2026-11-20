# Saved items on disk

How pinned items, and the history while "Remember history after restart" is
on, are written to disk and read back.

```ail
node SavedItems : data   level: system
  why   What survives a restart must survive a crash mid-write too, and must
        be readable on another platform and by a later version.   <- PinItems
  what
    PinnedItemStore(root: text, logger: Logger or nothing = nothing) -> PinnedItemStore
    PinnedItemStore.SaveAsync(item: ClipboardItem) -> later nothing
    PinnedItemStore.DeleteAsync(id: text) -> later nothing   — id is the item's Id, a GUID
    PinnedItemStore.LoadAllAsync() -> later list of ClipboardItem   — oldest pin first
    HistoryPersistence(history: ClipboardHistory, root: text, isEnabled: () -> boolean, logger: Logger or nothing = nothing) -> HistoryPersistence
    HistoryPersistence.LoadAsync() -> later integer   — the number of items restored
    HistoryPersistence.Start() -> nothing
    HistoryPersistence.Stop() -> nothing
    HistoryPersistence.RequestSync() -> nothing
    HistoryPersistence.FlushAsync() -> later nothing
    Pinned items are kept in AppPaths.PinnedDirectory, <data>/Pinned, and the history in AppPaths.HistoryDirectory, <data>/History.
      <data> is %LOCALAPPDATA%\Shapeshifter (Windows), ~/Library/Application Support/Shapeshifter (macOS), $XDG_DATA_HOME/shapeshifter (Linux), or $SHAPESHIFTER_HOME/data; created 0700 on Unix
    Each item is one folder, named by its id as 32 hex digits without hyphens, holding manifest.json (indented JSON) and one file per part:
      text.txt (UTF-8 with a byte order mark), image.bin, raw-000.bin, raw-001.bin … in clipboard order, source-icon.png
    manifest.json holds { Version: 1, Id, CapturedAt, TextFile?, ImageFile?, Files: list of text, RawFormats: list of { Name, File }, SourceApplication?, SourceWindowTitle?, SourceIconFile? }
      formats are keyed by name ("text/html", "HTML Format"), never by a platform's numeric id
    History/order.json holds a list of { Id, CapturedAt }: unpinned items only, top first
    examples
      SaveAsync(item "hi", plain text) => <root>/<id>/manifest.json with TextFile "text.txt", ImageFile nothing, RawFormats [], and <root>/<id>/text.txt
      SaveAsync(item) while <root>/<id> exists => written to <root>/<id>.saving, the old folder deleted, then the new one moved in
      LoadAllAsync() with a folder "<id>.saving" => that folder skipped
      LoadAllAsync() with a manifest of Version 2 => that item skipped
      LoadAllAsync() with RawFormats [{ Name "x", File "../x.bin" }] => that item skipped, a warning logged
      LoadAllAsync() with folders pinned at 10:00 and 09:00 => [the 09:00 one, the 10:00 one]
      sync, enabled, history [P pinned, A, B] => History holds folders A and B, order.json [A, B]; P only in Pinned
      sync, enabled, B deleted from the history => folder B deleted, order.json [A]
      sync, disabled => the History folder deleted with everything in it
      LoadAsync(), order.json [B, A], folders A, B and C (C newest, not listed) => restored [B, A, C], below the pinned items
      LoadAsync(), disabled => 0, nothing restored (the next sync deletes the folder)
  how
    decide write into "<id>.saving", then move the folder into place
      because a folder that exists is complete; a crash leaves only a ".saving" folder, which is skipped
    decide a manifest with unknown parts, missing lists or a file outside its folder is skipped, never trusted
      because a manifest must never make the app read files outside its item's folder (PinnedItemStore.Resolve)
    decide each change of the history schedules one sync on a background task; changes during a sync queue exactly one more
      because a burst of copies must not write the disk once per copy, nor block the clipboard
    decide a sync writes only items not on disk yet, deletes folders of items that are gone, then rewrites order.json through order.json.tmp
      because items never change once copied, so only the order needs rewriting
    decide on quitting, syncs are waited for at most 3 seconds
      because "unknown, confirm with the owner"
    decide I/O, permission and JSON errors are logged as warnings and the sync is skipped
      because "unknown, confirm with the owner"
    decide pinned order after a restart comes from each folder's last write time
      because "unknown, confirm with the owner: an explicit pin order was never stored"
  sources
    src/Shapeshifter.Core/Persistence/PinnedItemStore.cs; src/Shapeshifter.Core/Persistence/HistoryPersistence.cs
    src/Shapeshifter.Core/Settings/AppPaths.cs; src/Shapeshifter.Desktop/App.axaml.cs, StartAsync and Shutdown
    README.md, "Where things are"; docs/architecture.md, "Capturing copies"
    driven on Linux 2026-11-20: the Pinned/<id> folder (manifest.json, text.txt, source-icon.png) and History/order.json seen on disk
```
