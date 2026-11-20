# Boxed Sheet, the Linux look

```ail
node BoxedSheet : view   level: experience
  why   On Linux the picker should read as a GNOME libadwaita sheet, on every
        desktop (commit 3ab513b "Give Linux the Boxed Sheet look").   <- Looks
  what
    an opaque sheet with rounded corners, a hairline border and a soft shadow, without blur
    a header bar with the app's icon and "Shapeshifter" centred; no count and no hint in it
    the history and the actions are boxed lists: white cards of rows split by hairlines, under bold headings, "History" with the count and "Actions" with "→ to choose"
    the chosen row of the list that has the keyboard is tinted with the accent and ringed in it; the other list keeps a fainter tint (history) or only its accent title (actions)
    the default action is marked with a check mark; key caps are monospaced
    Scenario: the Linux picker
      Given "dotnet run --project src/App" and six other items were copied
      When the user holds Ctrl+V until the picker appears
      Then the header shows the clipboard icon and "Shapeshifter" centred
      And "History" is written above the history's card with "7 items" at the card's end
      And the chosen row is tinted pale blue and ringed in blue, its tile white with a blue icon
      And "Actions" and "→ to choose" head the actions' card, and "Paste" is blue with a blue check mark at its end
    Scenario: without a compositor
      Given a bare X server, as when driven here
      Then the sheet fills its window: square corners, a #C0BFBC border, no shadow
  how
    decide Adwaita Sans, then Cantarell, then the bundled Inter; key caps in Adwaita Mono
      because those are GNOME's fonts; Inter stands in where they are missing (Linux.axaml)
    decide no blur
      because GNOME has none for applications (Linux.axaml)
    decide the selection ring is inset by 2 inside the row
      because the card's rounded clip must never cut into it, wherever the row is scrolled to (Linux.axaml)
  look
    sheet       #FAFAFB, corners 12, a hairline of black 10 %, shadow 0 2 6 black 12 % and 0 14 36 black 27 %, 36 / 20 / 36 / 48 of room around it; opaque: square, a #C0BFBC border, no shadow
    header      46 high, 12 of side padding, centred: icon 20, 8 before "Shapeshifter" bold 15 #2E2E33
    body        18 of margin at the sides and bottom; the history 416 wide, 18 before the actions 284 wide
    headings    24 high, 6 in from the card's edges, 30 above the cards: bold 15 #2E2E33, the caption 12 #5E5C64 at the end
    card        white, corners 12, shadow 0 1 3 black 7 %, a hairline border of black 10 % drawn over the rows
    rows        64 high, 12 of side padding, split by hairlines of black 7 % (none above the first); hover black 3 %, pressed black 7 %
    chosen      the accent at 14 % behind the row (8 % while the actions have the keyboard); a ring of 2 in the accent's text shade, rounded 8, inset 2; the tile white, its icon in the accent; the title medium #1E1E22, the source #4A4850, the time #46454C, the pin in the darker accent
    tile        40 × 40, rounded 8, #F6F5F4 with a hairline of black 8 %; line icons 20, stroke 1.5, #3D3846; file icons 28; up to four file icons 14 in a two by two grid
    text        title 14 on 19 #2E2E33, one line; source 12.5 #5E5C64, at most 250 wide, after a 12 icon; time 12 #5E5C64; pin 13 in the accent
    actions     entries 12 / 11 / 12 / 12 of padding; icon 16 #3D3846, stroke 1.5; title 14 #2E2E33, semibold in the accent when chosen; description 12.5 on 17 #5E5C64; a 16 check mark in the accent for the default
    preview     white, corners 12, a hairline of black 7 %, 30 above, 12 below; strip #F6F5F4
    footer      a hairline of black 7 % above, 12 by 7 of padding, at least 40 high, hints centred 9 apart; key caps Adwaita Mono 11.5 #2E2E33 on white, rounded 6, border black 18 %, 1 / 1 / 1 / 2; labels 12.5 #5E5C64
    accent      the system's; #3584E4 when none (text shade #1C71D8, dark #255C9F)
  copy
    title           "Shapeshifter"
    heading.history "History"
    heading.actions "Actions"
    heading.choose  "→ to choose"
  sources
    commits 3ab513b "Give Linux the Boxed Sheet look", eb4495d
    driven on Linux (Xvfb, openbox, no compositor) 2026-11-20: the run-legacy instance and the German and Arabic ones
    assets/screenshots/linux.PNG, empty-linux.PNG
    src/Shapeshifter.Desktop/Styles/Linux.axaml
```
