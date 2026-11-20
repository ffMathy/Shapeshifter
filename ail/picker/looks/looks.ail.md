# The looks

Shapeshifter wears the design language of the system it runs on. The picker's
layout is the same everywhere; a look gives it material, sizes, colours, type
and icons.

```ail
node Looks : view   level: experience
  why   A clipboard manager is part of the desktop; it should look like the
        system's own flyouts on each platform rather than like a foreign app
        (commit "Prepare a look per operating system"; the system's accent: #164).   <- Picker
  what
    three looks: Mica Sheets on Windows, Refraction on the Mac, Boxed Sheet on Linux (every desktop)
    the look is chosen at start from the operating system; a setting in the environment can choose another, for development and screenshots
    the Windows look follows the system's light or dark mode, live; the Mac and Linux looks are always light
    every look takes the system's accent colour, in shades that stay readable, and falls back to its own
    where the system offers a frosted material (Windows 11, the Mac), the picker sits on it; without a compositor the picker is a square, opaque panel filling its window
    Scenario: the Linux look
      Given Shapeshifter runs on Linux
      When the user holds Ctrl+V until the picker appears
      Then the header shows the app's icon and "Shapeshifter" centred, and the history and actions are white cards under bold headings
    Scenario: another look chosen for development
      Given Shapeshifter was started on Linux with SHAPESHIFTER_LOOK set to "windows"
      Then the header says "Clipboard history" with the count at its end, and the chosen row has a short accent bar at its start edge
      Given it was started with SHAPESHIFTER_LOOK set to "macos" instead
      Then the header says "Clipboard" with the count in a small pill, and the chosen row is a capsule filled with the accent
    Scenario: the Windows look in dark mode
      Given the Windows look with SHAPESHIFTER_THEME set to "dark"
      Then the picker is near-black with white text, and the accent marks read on it
    Scenario: no compositor
      Given a desktop without a compositor, as on a bare X server
      Then the picker is a square panel with a 1-pixel border that fills its window, with no shadow and no rounded corners
  how
    decide one style file and one folder of icons per look, over one shared layout
      because Shapeshifter should match the design language of each operating system (commit "Prepare a look per operating system")
    decide SHAPESHIFTER_LOOK takes windows, macos or linux, in any case; anything else is ignored
      because it exists for development and screenshots (PlatformLook)
    decide only the Windows look has a dark variant
      because "unknown, confirm with the owner: are dark Mac and Linux looks wanted?" (PR #625 made Windows follow dark mode)
    decide the system's accent colour, as the original Shapeshifter did
      because #164 asked for the system's colour instead of always blue
    decide the panel goes square and opaque when the window cannot be see-through
      because otherwise opaque black corners would show around it (PickerViewModel.IsOpaque)
    decide on a Linux desktop that reports no accent the accent is the toolkit's default blue, shown as #006EC6, not the look's #3584E4
      because "unknown, confirm with the owner: keep this? the Linux look's own blue is only used for screenshots"
  copy
    title.windows    "Clipboard history"
    title.macos      "Clipboard"
    title.linux      "Shapeshifter"      never translated
  children  MicaSheets, Refraction, BoxedSheet, AccentShades, NativeMaterial
  sources
    issue #164 "Use Aero color for design"; PRs #618, #625, #626
    commits 3956ad5, 4b4439f, 12365ce, 3ab513b, fd387a4
    docs/architecture.md, "Looks"
    driven on Linux (Xvfb, openbox, no compositor) 2026-11-20: the Linux look; SHAPESHIFTER_LOOK=windows with SHAPESHIFTER_THEME light and dark, SHAPESHIFTER_LOOK=macos, each on a display of its own; the accent sampled at #006EC6
    assets/screenshots/windows.PNG, macos.PNG, linux.PNG, blue.PNG, green.PNG, purple.PNG, red.PNG
    src/Shapeshifter.Desktop/Services/PlatformLook.cs, AccentPalette.cs, NativeMaterial.cs
```
