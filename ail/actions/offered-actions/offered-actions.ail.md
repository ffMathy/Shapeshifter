# Which actions an item offers

Each kind of item offers only the actions that make sense for it, always in
the same order: Paste first, Pin last.

```ail
node OfferedActions : flow   level: experience
  why   An action that cannot work on an item is noise in a list the user
        reads while holding keys; the list must be short and its order fixed,
        so the keys to reach an action can be learnt (#62).   <- Actions
  what
    Scenario: plain text offers Paste, Paste as file and Pin
      Given a plain text was copied
      When the user holds Ctrl+V until the picker appears and taps Right
      Then the actions are "Paste", "Paste as file", "Pin to clipboard", in that order
    Scenario: formatted text also offers Paste as plain text
      Given a text with formatting (bold, from a web page) was copied
      When the user opens its actions
      Then they are "Paste", "Paste as file", "Paste as plain text", "Pin to clipboard"
    Scenario: an image offers Paste, Paste as file, Upload and paste link, and Pin
      Given an image was copied
      When the user opens its actions
      Then they are "Paste", "Paste as file", "Upload and paste link", "Pin to clipboard"
    Scenario: a file offers Paste, Copy as compressed folder and Pin
      Given one file was copied in a file manager
      When the user opens its actions
      Then they are "Paste", "Copy as compressed folder", "Pin to clipboard"
    Scenario: several files offer the same as one file
      Given two files were copied together
      When the user opens their actions
      Then they are "Paste", "Copy as compressed folder", "Pin to clipboard"
    Scenario: other data offers only Paste and Pin
      Given something that is neither text, an image nor files was copied
      When the user opens its actions
      Then they are "Paste", "Pin to clipboard"
    Scenario: text with links also offers the link actions
      Given the text "see https://example.com/a.png and https://example.org" was copied
      When the user opens its actions
      Then they are "Paste", "Paste as file", "Copy image from link", "Open links", "Pin to clipboard"
  how
    decide one fixed order for every kind: Paste, Paste as file, Paste as plain text, Upload and paste link, Copy as compressed folder, Copy image from link, Open link or Open links, then Pin or Unpin
      because Paste is the default and comes first; "unknown, confirm with the owner: why this order for the rest"
    decide a pinned item offers "Unpin from clipboard" in place of "Pin to clipboard", in the same place
      because one action toggles, so pinning and unpinning sit under the same keys
    decide the descriptions say what the action does with this item: ".txt" for text, the image's own type (".png", ".jpeg") for an image, one link or several
      because each description is written for the item it is shown with (#62 "Have description of actions be dynamically generated")
    decide other data is headed "Actions for this text" in the Windows and Mac looks
      because "unknown, confirm with the owner: keep this?"
    decide which actions an item offers is worked out once, when it is copied
      because the list is needed on the keyboard hook's thread while the gesture runs, where nothing slow may happen
  copy
    action.paste            "Paste"                        "Pastes clipboard contents as-is."
    action.pasteFile        "Paste as file"                "Saves this text to a .txt file and pastes the file." / "Saves this image to a .{ext} file and pastes the file."
    action.plain            "Paste as plain text"          "Pastes clipboard contents as plain text with no formatting."
    action.upload           "Upload and paste link"        "Uploads this image to Catbox and pastes a public link to it."
    action.zip              "Copy as compressed folder"    "Copy these files to the clipboard as a compressed (.zip) folder."
    action.imageFromLink    "Copy image from link"         "Downloads the linked image and copies the image itself to the clipboard."
    action.openLink         "Open link" / "Open links"     "Opens the link in your browser." / "Opens the links in your browser (at most 10)."
    action.pin              "Pin to clipboard"             "Pins this item so that it always stays at the top, and saves it to your computer so that it is there the next time you open Shapeshifter."
    action.unpin            "Unpin from clipboard"         "Unpins this item so that it moves down as you copy, and is no longer saved to your computer."
    heading                 "Actions"   with "→ to choose" beside it in the Linux look; the Windows and macOS looks head the list "Actions for this text", "Actions for this image", "Actions for this file" or "Actions for these files" (other data, too, reads "Actions for this text")
    default                 "Default"   the badge on Paste in the Windows and macOS looks; a tick in the Linux look
  uses      ActionCatalog
  sources
    GitHub issue #62; PR #629 "Image actions (paste as file, upload to Catbox) and a website Actions section"
    driven on Linux (Xvfb, openbox) 2026-11-20: plain text, HTML text, a PNG, one file, two files, a custom format and text with two links put on the clipboard with xclip; actions read from screenshots
    src/Shapeshifter.Core/Actions/ActionCatalog.cs, CreateDefault and GetAvailable
    src/Shapeshifter.Core/Localization/Strings.resx, Action* and PickerActionsFor*
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, UpdateActionsHeader; src/Shapeshifter.Desktop/Styles/Linux.axaml, "→ to choose"
```
