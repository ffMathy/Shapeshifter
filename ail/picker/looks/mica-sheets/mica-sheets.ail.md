# Mica Sheets, the Windows look

```ail
node MicaSheets : view   level: experience
  why   On Windows 11 the picker should read as one of the shell's own flyouts,
        like the emoji panel or Win+V's clipboard history, in light and dark
        (commits "Give Windows the Mica Sheets look", PR #625).   <- Looks
  what
    a near-opaque flyout with rounded corners, a hairline and the flyout shadow, or the window itself on the system's material
    a header with the icon, "Clipboard history" and the count at the end, over a divider
    the history on the flyout, the actions on a lighter panel beside it, the keys on a slightly darker band
    the accent marks only what is about to happen: a short pill beside the chosen item of the pane that has the keyboard, and the icon of the chosen action
    text rows show two lines of text
    light and dark follow the system's app mode, live; SHAPESHIFTER_THEME set to light or dark overrides it
    Scenario: light
      Given the Windows look in light mode and three items copied
      When the user holds Ctrl+V until the picker appears
      Then the header says "Clipboard history" and "3 items" at its end
      And the chosen row has a light grey fill and a 3 × 16 accent pill at its start edge
      And the actions panel says "Actions for this text", with "Default" at the end of "Paste"
    Scenario: dark
      Given the Windows look with SHAPESHIFTER_THEME set to "dark"
      Then the flyout is #202020 without a compositor, the text white, the chosen row a faint white fill, and the pill a lighter shade of the accent
  how
    decide Segoe UI Variable, then Segoe UI, then the bundled Inter
      because it is the system's type; Inter stands in where it is missing
    decide every colour a theme resource with a light and a dark value
      because a change of the app mode must restyle open windows (PR #625)
    decide the pill and focus marks use the accent's text shade
      because light accents such as gold #FFB900 are under 3:1 on these surfaces (Windows.axaml)
  look
    flyout      corners 8, a 1-pixel stroke, shadow 0 16 32 black 18 % and 0 2 8 black 8 %, 32 / 16 / 32 / 48 of room around it for the shadow; fill light #F2F7F7F7, dark #F22C2C2C; stroke light black 10 %, dark white 12 %
    opaque      light #F3F3F3 with a #C4C4C4 border; dark #202020 with #454545; square, no shadow
    header      44 high, 16 of side padding, a divider below; icon 16, 10 before the title 14 semibold; count 12, secondary text
    history     432 wide, 4 of padding; rows 60 high with 2 between, rounded 4; the tile 40 × 40, rounded 4, 9 from the pill, a white (dark: white 6 %) fill and a hairline
    title       14 on 19, two lines for text; second line 12 secondary after a 12 icon, at most 330 wide; time 11 secondary, pin 12
    selected    fill black 6 % (dark white 8 %); black 3 % (dark white 5.5 %) while the actions have the keyboard, without the pill
    actions     300 wide, 4 of padding, a hairline on the start side, fill white 55 % (dark white 4 %), opaque #F9F9F9 (dark #282828); heading 12 semibold secondary; entries rounded 4, 12 by 9 of padding, the chosen one the selected fill
    footer      at least 40 high, 16 by 8 of padding, fill #EEEEEE at 55 % (dark #1C1C1C at 55 %), opaque #EEEEEE (dark #1C1C1C); hints 18 apart; key caps 11 on white (dark white 9 %), rounded 4, border black 16 % (dark white 12 %)
    text        primary #1A1A1A (dark #FFFFFF), secondary #5D5D5D (dark white 78 %), body #3B3B3B (dark white 87 %)
    accent      the system's; #0067C0 when it reports none
  copy
    title      "Clipboard history"
    default    "Default"
  sources
    commits 4b4439f "Give Windows the Mica Sheets look", 8edbbbd, 2ef3ece, c90fef0; PRs #625, #626
    driven on Linux (Xvfb, openbox) 2026-11-20 with SHAPESHIFTER_LOOK=windows and SHAPESHIFTER_THEME light, then dark, without a compositor
    assets/screenshots/windows.PNG, blue.PNG, green.PNG, purple.PNG, red.PNG, pad.PNG
    src/Shapeshifter.Desktop/Styles/Windows.axaml
```
