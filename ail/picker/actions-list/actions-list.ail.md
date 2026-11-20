# How the actions look

The list of what can be done with the chosen item, below its preview. What
each action does, and which actions an item gets, belong to the actions area;
this node is only their appearance.

```ail
node ActionsList : view   level: experience
  why   The user must see what letting go of Ctrl will do, and what else could
        be done, before choosing (PR #618: titles and descriptions as in the
        original Shapeshifter).   <- Picker
  what
    a heading, then one entry per action: a line icon, a title and a description that wraps
    the first action is the default, the one letting go of Ctrl runs, and is marked as such
    the chosen action is marked; while the actions have the keyboard (after Right) the mark is stronger, and the history row keeps a fainter mark
    the heading names the kind of item in the Windows and Mac looks
    Scenario: the actions of a text
      Given "dotnet run --project src/App" is chosen
      Then the actions are "Paste" with "Pastes clipboard contents as-is.", "Paste as file" with "Saves this text to a .txt file and pastes the file.", and "Pin to clipboard" with its description
      And "Paste" is marked as the default with a check mark, its title and icon in the accent colour
    Scenario: moving into the actions
      Given the picker is up with an image chosen
      When the user taps Right
      Then "Paste" is ringed in the accent colour and the image's row keeps only a faint tint
    Scenario: the heading in the Windows look
      Given the Windows look and a text chosen
      Then the heading says "Actions for this text", and "Default" is written at the end of "Paste"
  how
    decide the accent marks only what is about to happen: the default and the chosen action
      because everything else stays in the system's neutral palette (commit "Give Windows the Mica Sheets look")
    decide in the Linux look the selected action stays white, with only its title and icon in the accent
      because the accent title keeps 4.5:1 contrast only on plain white (Linux.axaml)
    decide the Linux look writes "Actions" and "→ to choose" in English in every language
      because "unknown, confirm with the owner: keep this? the words are fixed in the Linux look's template"
  look
    pane        Linux 284 wide, no background, below the preview card; Windows 300 wide, a translucent tint with a hairline on its start side, 4 of padding; macOS 250 wide, a nested glass sidebar rounded 18 with 8 of padding
    heading     Linux: "Actions" bold 15 and "→ to choose" 12 dim on the same line, 24 high; Windows: the kind of item, 12 semibold dim; macOS: the same, 11 semibold
    entry       a 16 line icon at the top, 12 of space, the title 14 (macOS 13 semibold) and the description 12.5 dim (Windows 12, macOS 11 on at most 3 lines)
    Linux       a white card of entries split by hairlines, rounded 12; the chosen entry's title semibold in the accent, its icon in the accent; a 2-pixel accent ring while the actions have the keyboard; the default marked by a 16 check mark in the accent at the end
    Windows     entries rounded 4 with 2 between them; the chosen one a subtle fill, and a 3 × 16 accent pill at its start while the actions have the keyboard; its icon in the accent; "Default" 11 dim at the end
    macOS       each entry a card; the chosen one near-white with an accent outline (at 55 % while the history has the keyboard); icons in the accent inside a round well; "Default" in a small grey pill, 10 semibold
  copy
    heading           "Actions"
    heading.linux     "→ to choose"          Linux look only, never translated
    heading.text      "Actions for this text"
    heading.image     "Actions for this image"
    heading.file      "Actions for this file"
    heading.files     "Actions for these files"
    default           "Default"
  sources
    PR #618 "the original picker design"; commits "Give Windows the Mica Sheets look", "Give macOS the Refraction look", "Give Linux the Boxed Sheet look"
    driven on Linux (Xvfb, openbox) 2026-11-20: text, image, file and files actions; Right into the actions; the Windows and Mac looks on displays of their own
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml, "actionsPane"; src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, UpdateActionsHeader
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml, MacOS.axaml, "actions"
    assets/screenshots/actions.PNG
```
