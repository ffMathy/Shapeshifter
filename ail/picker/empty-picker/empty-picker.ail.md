# The empty picker

```ail
node EmptyPicker : view   level: experience
  why   Holding Ctrl+V with nothing copied used to show nothing at all; a first
        try should explain how Shapeshifter works instead (#144, PR #623).   <- Picker
  what
    with nothing copied since Shapeshifter started (and nothing pinned or remembered), holding Ctrl+V still shows the picker
    in place of the history it explains how to use Shapeshifter; there is no list, no preview and no actions
    the item count is not shown, and letting go of Ctrl pastes as it would without Shapeshifter
    the explanation names the paste shortcut's modifier: Ctrl, its local name, or Cmd on the Mac
    Scenario: nothing copied yet
      Given Shapeshifter has just started and nothing was copied
      When the user holds Ctrl+V until the picker appears
      Then the picker shows "Nothing copied yet" and below it "Copy as usual, and Shapeshifter keeps everything you copy."
      And then "To paste something you copied earlier, hold Ctrl+V until the history appears. Keep holding Ctrl, press V or the arrow keys to choose, and release Ctrl to paste. Press Space to keep the history open without holding the key, then Enter to paste."
      And the footer shows the keys as for a picker with items
    Scenario: in German
      Given the display language is German
      Then the picker shows "Noch nichts kopiert", and the explanation names "Strg"
  how
    decide explain the gesture in the picker itself rather than show an empty list
      because #144 asked for a "tutorial-ish" screen when nothing has been copied
    decide the explanation ends with keeping the history open with Space, then Enter
      because keeping it open is part of the gesture since #420 (PR #632); its sentence was added then, in all 21 languages (commit "Translate the kept-open picker's strings into all 21 languages")
  look
    window      only as wide as the explanation: 472 × 405 in the Linux look on a screen without a compositor
    header      as with items; the count is hidden in the Windows and Mac looks
    text        the title bold, 17 (Windows 20 semibold), the body 14 on 20 (Mac 13 on 18), dim, wrapped; 8 between them; the block 392 wide in the Linux look (Windows 400, Mac 390) with 12 by 18 of margin
    footer      the same hints as with items
  copy
    title     "Nothing copied yet"
    hint      "Copy as usual, and Shapeshifter keeps everything you copy.\n\nTo paste something you copied earlier, hold {modifier}+V until the history appears. Keep holding {modifier}, press V or the arrow keys to choose, and release {modifier} to paste."
    hint.open " Press Space to keep the history open without holding the key, then Enter to paste."     appended to hint
  sources
    issue #144 "If no items have been copied, show a different tutorial-ish UI"; PR #623
    driven on Linux (Xvfb, openbox) 2026-11-20: the empty picker in English, German and Arabic
    assets/screenshots/empty-linux.PNG, empty-windows.PNG, empty-macos.PNG
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, IsEmpty and CreateEmptyHint
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml, MacOS.axaml, "emptyState"
```
