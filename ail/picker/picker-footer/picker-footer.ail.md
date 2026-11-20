# The key hints

```ail
node PickerFooter : view   level: experience
  why   The picker is driven only with keys; the keys must be shown where the
        user is looking, since nobody reads a manual for a paste shortcut
        (README, "Hold Ctrl+V").   <- Picker
  what
    a footer of hints, each one or two key caps and what they do, in reading order; they wrap onto more lines rather than widen the picker
    the hints change with the state of the picker: while Ctrl is held, while it is kept open, and while searching
    the modifier's key cap is the local name of Ctrl, or "⌘" for Cmd, or "Win"
    Scenario: while Ctrl is held
      Given something was copied
      When the user holds Ctrl+V until the picker appears
      Then the footer shows "Ctrl" "Release to paste", "Space" "Keep open", "V" "↑↓" "Move", "1–9" "Jump", "←→" "Actions", "S" "Search", "Del" "Remove", "Esc" "Cancel"
    Scenario: while it is kept open
      When the user taps Space
      Then the footer shows "Enter" "Paste", "V" "↑↓" "Move", "1–9" "Jump", "←→" "Actions", "S" "Search", "Del" "Remove", "Esc" "Close"
    Scenario: while searching
      When the user taps S
      Then the footer shows "Enter" "Paste", "↑↓" "Move", "←→" "Actions", "Esc" "Cancel"
    Scenario: the Mac look
      Given the Mac look with the paste shortcut on Cmd
      Then the first key cap is "⌘", and the header says "Release ⌘ to paste", or "Press Enter to paste" once kept open
  how
    decide the footer is measured no wider than the history and the actions together
      because long translations must wrap rather than widen the picker (PickerWindow.axaml)
    decide key caps carry the local key names, such as "Strg" and "Entf" in German
      because the translations name the keys as printed on local keyboards (PR #633)
    decide the arrows and "1–9" are drawn as they are, even in right-to-left languages, where the row is mirrored
      because "unknown, confirm with the owner: in Arabic the caps read 9–1 and ↓↑; keep this?"
  look
    footer      Linux: a hairline above, 12 by 7 of padding, at least 40 high, the hints centred, 9 apart; Windows: a slightly darker band, the hints from the start edge, 18 apart; macOS: a hairline above, from the start edge, 16 apart
    key cap     Linux: Adwaita Mono 11.5 on white, rounded 6, a 1-pixel border 2 at the bottom, at least 22 wide, 6 of side padding; Windows: 11, rounded 4, the same border; macOS: 11 semibold on near-white, rounded 6, 20 high, a hairline shadow
    label       Linux 12.5 dim, 5 after its key caps; Windows 12 dim; macOS 11.5
  copy
    hint.release     "Release to paste"
    hint.keepopen    "Keep open"
    hint.paste       "Paste"
    hint.move        "Move"
    hint.jump        "Jump"
    hint.actions     "Actions"
    hint.search      "Search"
    hint.remove      "Remove"
    hint.cancel      "Cancel"
    hint.close       "Close"
    key.ctrl         "Ctrl"
    key.cmd          "⌘"
    key.space        "Space"
    key.enter        "Enter"
    key.delete       "Del"
    key.escape       "Esc"
    keys             "V"  "↑↓"  "1–9"  "←→"  "S"
    header.release   "Release {key} to paste"      Mac look only
    header.kept      "Press Enter to paste"        Mac look only
  sources
    README.md, "Hold Ctrl+V"; issue #420 (kept open), #458 (search); PRs #632, #633
    driven on Linux (Xvfb, openbox) 2026-11-20: held, kept open; the Windows, Mac, German and Arabic pickers on displays of their own
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, "footer"; src/Shapeshifter.Desktop/Views/WidthFollower.cs
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, ModifierKey, ReleaseHint, EscapeHint
```
