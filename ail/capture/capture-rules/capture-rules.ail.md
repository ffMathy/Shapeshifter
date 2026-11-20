# Capture rules

From "the clipboard changed" to an item in the history: debouncing, ignoring
Shapeshifter's own writes, and adding or moving the item.

```ail
node CaptureRules : fn   level: system
  why   Capturing runs on every copy the user makes in any app, so its
        timing and duplicate rules must be exact and testable without a desktop.   <- Capture
  what
    ClipboardMonitor(backend: IClipboardBackend, changes: IClipboardChangeSource, history: ClipboardHistory, logger: Logger or nothing = nothing, sourceLocator: IClipboardSourceLocator or nothing = nothing) -> ClipboardMonitor
    ClipboardMonitor.Start() -> nothing
    ClipboardMonitor.CaptureNowAsync(source: ClipboardSource or nothing = nothing) -> later ClipboardItem or nothing
    ClipboardMonitor.WriteAsync(snapshot: ClipboardSnapshot) -> later nothing — Shapeshifter's own clipboard writes go through WriteAsync
    ClipboardMonitor.Captured — an event carrying (item, isNew, source)
    ClipboardMonitor.Debounce: duration = 100 ms
    ClipboardMonitor.SelfWriteGracePeriod: duration = 750 ms
    ClipboardHistory(maxUnpinnedItems: integer = 50) -> ClipboardHistory
    ClipboardHistory.Add(snapshot: ClipboardSnapshot, source: ClipboardSource or nothing = nothing) -> { item: ClipboardItem, isNew: boolean }
    in the examples, change(t) is a change notice at t ms; history [a, b] is top first, *p a pinned item
    examples
      change(0) => the source is located at 0 ms, the clipboard is read at 100 ms, one capture
      change(0), change(50) => the first capture is cancelled; source located at 50, read at 150, one capture
      read gives an empty snapshot => nothing captured, no event
      WriteAsync(s) in progress, change => ignored (the grace runs until 750 ms after the write completes)
      WriteAsync(s) completed 800 ms ago, change, read gives s again => ignored (same fingerprint as the last write)
      WriteAsync(s), then the user copies s in another app minutes later, nothing captured in between => ignored
      history [b, a], Add(a) => [a, b], isNew false, a's copy time updated
      history [*p, b], Add(c) => [*p, c, b], isNew true
      history [*p, b], Add(p) => [*p, b] unchanged, isNew false
      history [b, a] without a source on a, Add(a, source Editor) => a's source becomes Editor; a source already known is kept
      max 3, history [c, b, a], Add(d) => [d, c, b]; a is removed
      max 3, history [*p, c, b], Add(a) => [*p, a, c, b]; pinned items do not count
      MaxUnpinnedItems = 1 on [*p, c, b] => [*p, c]
      StartAsync() with text on the clipboard => it is captured once, before the keyboard hook starts; NotifyCopied, and the coach only remembers it
      Captured(isNew: true) and no tip => NotifyCopied(item, history.Count), the count includes pinned items
      Captured(isNew: false) and no tip => nothing shown
      a tip from the coach => NotifyTip(tip) instead of NotifyCopied
  how
    decide the source app is located when the change is noticed, before the debounce
      because the user has had the least time to switch away from the app they copied from
    decide self-writes are recognised by fingerprint, plus a grace period after each write
      because a "skip the next notice" flag races on polling platforms, and some systems re-encode data (images) so the read-back differs
    decide an ignored read becomes the remembered self-write fingerprint
      because "unknown, confirm with the owner: keep this? A user copying, in another app, exactly what was last pasted from the history is never captured until something else is"
    decide captures are serialized and failures are logged and dropped
      because "unknown, confirm with the owner"
    decide the fingerprint is computed before taking the history's lock
      because hashing a large image takes milliseconds (commit b069ac4)
  children  ClipboardFormats, ChangeSources, CopySource
  sources
    src/Shapeshifter.Core/Clipboard/ClipboardMonitor.cs
    src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, StartAsync and OnCaptured
    src/Shapeshifter.Desktop/App.axaml.cs (wiring: ClipboardHistory from MaxHistoryItems, the monitor with the source locator)
    commit b069ac4 "Make clipboard fingerprints unambiguous and cheaper"
    docs/architecture.md, "Capturing copies"
```
