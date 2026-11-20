# The saved settings

The record behind the settings window, its defaults and limits, and how it is
kept on disk.

```ail
node Preferences : data   level: system
  why   Every setting needs one default, one valid range and one place on disk, so
        a hand-edited or older file can never break the app.   <- SettingsWindow
  what
    type PasteShortcut = "Standard" | "Alternative"
      saved as a number: Standard is 0, Alternative is 1
    type ShapeshifterSettings = { PasteDelayMilliseconds: integer, PasteShortcut: PasteShortcut, QuietMode: boolean, CoachingTips: boolean, MaxHistoryItems: integer, RememberHistory: boolean, AutomaticUpdates: boolean }
      PasteDelayMilliseconds starts at 300, clamped to 0 through 3000 (MaxPasteDelayMilliseconds)
      PasteShortcut starts at Standard
      QuietMode starts at false, and true hides the "Copied" notification (tips still show)
      CoachingTips starts at true
      MaxHistoryItems starts at 50, clamped to 1 through 1000, and counts unpinned items only
      RememberHistory starts at false
      AutomaticUpdates starts at true
    defaultSettings() -> ShapeshifterSettings — every value at its starting value above
    ShapeshifterSettings.Normalize() -> ShapeshifterSettings — a copy with the clamps above, and a PasteShortcut number that names neither choice replaced by Standard
    SettingsStore(path: text, logger: Logger or nothing = nothing) -> SettingsStore
    SettingsStore.Current: ShapeshifterSettings — loaded and normalized once, when the store is made
    SettingsStore.Update(change: (ShapeshifterSettings) -> ShapeshifterSettings) -> nothing — normalizes; does nothing when the result equals Current; else saves, then raises Changed
    SettingsStore.Changed — an event carrying the new ShapeshifterSettings, raised after saving, outside the lock
    examples
      (defaultSettings() with PasteDelayMilliseconds 5000).Normalize().PasteDelayMilliseconds => 3000
      (defaultSettings() with PasteDelayMilliseconds -1).Normalize().PasteDelayMilliseconds => 0
      (defaultSettings() with MaxHistoryItems 0).Normalize().MaxHistoryItems => 1;  with MaxHistoryItems 5000 => 1000
      (defaultSettings() with PasteShortcut saved as the number 7).Normalize().PasteShortcut => Standard
      SettingsStore over a missing file => Current is defaultSettings() by value; nothing is written until the first change
      SettingsStore over "{ not json" => defaults, and a warning in the log; the bad file is overwritten by the next change
      file {"PasteDelayMilliseconds": 900} => 900 with every other value at its default
      Update(a change that sets QuietMode to true) twice => saved once, Changed raised once
    the settings are kept in settings.json in the settings folder (DataLocations), indented JSON with the property names above, e.g.
      { "PasteDelayMilliseconds": 300, "PasteShortcut": 0, "QuietMode": false, "CoachingTips": true,
        "MaxHistoryItems": 50, "RememberHistory": false, "AutomaticUpdates": true }
      written to settings.json.tmp, then moved over settings.json; a failed save is logged and the change still applies
    applying a change (App.StartAsync, on Changed)
      the gesture's show delay = PasteDelayMilliseconds; the keyboard hook's shortcut, and the picker's key name, when PasteShortcut changed;
      history.MaxUnpinnedItems = MaxHistoryItems (drops the oldest unpinned items at once); the saved history syncs (RememberHistory)
      QuietMode and CoachingTips are read each time a notification or tip would show; AutomaticUpdates before each update check
  how
    decide out-of-range values are clamped, never rejected
      because a hand-edited or older file must still start the app
    decide the shortcut is saved as a number and unknown numbers fall back to the standard keys
      because a file from a newer version must not break an older one (PR #636)
    decide "Start at login" is not in this record; it is the autostart entry itself (StartAtLogin)
      because "unknown, confirm with the owner"
  sources
    src/Shapeshifter.Core/Settings/ShapeshifterSettings.cs and SettingsStore.cs
    src/Shapeshifter.Desktop/App.axaml.cs, the Changed handler in StartAsync
    src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs, MaxUnpinnedItems
    PR #636; PR #554; commit b3f57d8
    driven on Linux 2026-11-20: settings.json after typing 1500 as the delay (only in driving; the checks never read it)
```
