# Choosing the shortcut (Windows and macOS)

The "Shortcut" row: another key to hold instead of the paste keys. Linux has
none, so this is described from the code and the pull request, not driven.

```ail
node ShortcutChoice : data   level: system
  why   Some people want the history on a key of its own and the paste keys left
        exactly as they were: Win+V on Windows, in place of the Windows clipboard
        history (#28).   <- SettingsWindow
  what
    KeyMap.ShortcutModifierNames: list of text   — macOS ["Cmd", "Ctrl"]; Windows ["Ctrl", "Win"]; Linux ["Ctrl"]
    KeyMap.ForCurrentPlatform(shortcut: PasteShortcut) -> KeyMap   — Alternative counts only on Windows and macOS (SupportsAlternative)
    KeyMap.ModifierName: "Ctrl" | "Cmd" | "Win"   — the key the gesture is held with
    KeyMap.GestureModifierPastes: boolean   — false with the alternative shortcut: the paste then waits for the modifier's release
    SettingsViewModel.CanChoosePasteShortcut: boolean   — true when ShortcutModifierNames holds more than one name; the row and its divider are hidden otherwise
    SettingsViewModel.PasteShortcutOptions: list of text   — each of ShortcutModifierNames through Localizer.ModifierKey, followed by "+V"
    examples
      Windows: options => ["Ctrl+V", "Win+V"], description => "Win+V replaces the Windows clipboard history, so Ctrl+V pastes as usual"
      macOS: options => ["Cmd+V", "Ctrl+V"], description => "Ctrl+V leaves Cmd+V free to paste as usual"
      Linux: CanChoosePasteShortcut => false (the row is not shown)
      Linux: KeyMap.ForCurrentPlatform(Alternative).Shortcut => Standard (a saved Alternative is ignored)
      Localizer.ModifierKey("Ctrl") => the translated Ctrl key name (Strings.KeyControl); ModifierKey("Win") => "Win"
      Windows, Alternative: ModifierName => "Win"; the settings usage reads "…hold Win+V until the history appears, then keep Win held…"
      choosing an option => settings.Update(PasteShortcut = index); App sets keyboard.PasteShortcut and picker.SetModifierName at once
    With the alternative shortcut:
      the gesture works the same with either key; pasting still sends Ctrl+V (Cmd+V on macOS)
      a quick press pastes on the modifier's release, not on V's (PasteOnTriggerRelease off), and the history also appears
        while only the modifier stays held (see GestureRules)
      Windows: the first time a key is suppressed while Win is held, a Ctrl press is sent, so releasing Win does not open the Start menu
      the texts that name the key (usage, delay description, picker footer, tips) follow without a restart
  how
    decide Win+V on Windows and Ctrl+V on macOS, and nothing on Linux
      because Super+V belongs to the desktop on X11 (PR #636)
    decide a choice of two, not a free shortcut editor
      because "unknown, confirm with the owner: #28 asked to edit shortcuts freely"
    decide pasting while Win is held waits for its release
      because pasting while Win is held would send Win+Ctrl+V, another shortcut (PR #643)
    decide the Start menu is masked with a Ctrl press
      because Windows opens it when Win is released with no other key seen, and the V was suppressed (PR #636)
  sources
    issue #28 "Edit keyboard shortcuts"; PR #636 "Choose the shortcut for the history: Win+V on Windows, Ctrl+V on macOS"; PR #643; commit 8bded19
    src/Shapeshifter.Platform/Input/KeyMap.cs; src/Shapeshifter.Platform/Input/SharpHookKeyboardService.cs, MaskStartMenuAsync
    src/Shapeshifter.Desktop/ViewModels/SettingsViewModel.cs, the constructor and OnPasteShortcutIndexChanged
    docs/architecture.md, "The paste gesture"; README.md, "Hold Ctrl+V"
    not driven: Windows and macOS only; the PR asks for a manual check on real hardware
```
