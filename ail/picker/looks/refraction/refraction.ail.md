# Refraction, the Mac look

```ail
node Refraction : view   level: experience
  why   On the Mac (version 26, Tahoe) the picker should read as a sheet of the
        system's Liquid Glass, with the system's type and accent (commit 12365ce;
        the "Refraction" design board).   <- Looks
  what
    a floating sheet of frosted glass with large rounded corners and a soft shadow
    a header with the icon, "Clipboard", the count in a small pill, and at its end the paste hint
    the chosen row is a capsule filled with the accent, with white text and a chevron toward the actions
    the actions sit in a nested glass sidebar; each is a card, the chosen one outlined with the accent; the default carries a "Default" pill
    the second line of a row names its source in words only, without an icon
    Scenario: the Mac picker
      Given the Mac look, the paste shortcut on Cmd and five items copied
      When the user holds Cmd+V until the picker appears
      Then the header says "Clipboard", "5 items" in a pill, and "Release ⌘ to paste" at its end
      And the chosen row is an accent capsule with a chevron at its end
    Scenario: kept open
      When the user taps Space
      Then the header's hint says "Press Enter to paste"
    Scenario: without the system's material
      Given a desktop without a compositor
      Then the sheet is square and opaque, #F5F5F8, filling its window
  how
    decide the system font through its hidden name ".AppleSystemUIFont"
      because SF Pro is not installed under its own name; asking for it gave Helvetica Neue (commit 369addb)
    decide the system's frosted popover material by default, Liquid Glass only when asked for
      because Avalonia's own blur frosted the whole rectangular window as a flat grey box (MacOS.axaml)
      because "unknown, confirm with the owner: why is Liquid Glass not the default on macOS 26?"
    decide the accent is the system's, Prism Indigo #3A5CF0 when it reports none
      because the look's design board uses it (MacOS.axaml)
  look
    sheet       corners 24, 10 of padding, a 1-pixel border from white to white 60 % to white 70 % top to bottom, a fill from white 90 % to #FBFBFD at 82 %, inner highlights white at the top and white 35 % at the bottom, shadow 0 26 56 #281E5A at 28 % and 0 2 8 at 12 %, 36 / 22 / 36 / 52 of room around it
    on glass    no room around it and no drawn shadow; a fill from white 40 % to #FBFBFD at 25 % over the system's material
    header      36 high, 8 of side padding, 8 below; icon 24, 6 before the title 14 semibold #1D1D1F; the count 11 semibold #3A3A40 in a round pill of white 85 % with a hairline; the hint 12 #3A3A40
    history     414 wide, 10 before the actions; rows 64 with a capsule rounded 16, 8 / 8 / 32 / 8 of padding; hover white 45 %
    chosen      the capsule rounded 30, filled with the accent's text shade, an inner highlight; title, source, time and pin in white; a white 8 × 14 chevron 16 from the end; while the actions have the keyboard the soft accent instead, the text dark
    tile        44, rounded 11, 12 before the text; text and files on a glass tile of white 80 % with a hairline; the line icon 22 in the accent; on the chosen text row a circle of white 22 % with a white icon
    text        title 13 semibold #1D1D1F, one line; source 11 #4A4A52; time 11 #55555C; pin 12
    actions     250 wide, 8 of padding, rounded 18, white-ish #F7F7FB at 50 % with a white 70 % border; heading 11 semibold #4A4A52; entries cards rounded 14, the chosen white 90 % with a 1.5 accent outline (55 % while the history has the keyboard); icons 17 in the accent inside a 28 round well; title 13 semibold, description 11 #4A4A52 on at most 3 lines
    preview     white 95 %, rounded 12, a hairline; monospaced SF Mono, Menlo
    footer      a hairline above, 8 / 7 / 8 / 0 of padding; key caps 20 high, rounded 6, white 85 % with a hairline shadow, 11 semibold #1D1D1F; labels 11.5 #3A3A40; hints 16 apart
  copy
    title          "Clipboard"
    hint.release   "Release ⌘ to paste"
    hint.kept      "Press Enter to paste"
    default        "Default"
  sources
    commits 12365ce "Give macOS the Refraction look", dcb0872, 369addb, 33079ea
    driven on Linux (Xvfb, openbox) 2026-11-20 with SHAPESHIFTER_LOOK=macos, without a compositor (opaque sheet, Ctrl as the modifier)
    assets/screenshots/macos.PNG, empty-macos.PNG
    src/Shapeshifter.Desktop/Styles/MacOS.axaml
```
