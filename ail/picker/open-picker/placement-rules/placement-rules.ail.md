# Placement rules

```ail
node PlacementRules : fn   level: system
  why   Where the picker goes and how much history it shows must be exact on
        every screen size, scaling and monitor layout.   <- OpenPicker
  what
    TopFraction: number = 0.16
    HeightFraction: number = 0.62
    FindScreen(screens: list of Screen, pointer: PixelPoint or nothing) -> Screen or nothing
    MaxListHeight(workingArea: PixelRect, scaling: number, rowHeight: number) -> number
    FitsOnScreen(pickerHeight: number, workingArea: PixelRect, scaling: number) -> boolean
    Position(workingArea: PixelRect, pickerSize: Size, scaling: number) -> PixelPoint
    Scaling(window: Window, screen: Screen) -> number   — the screen's scaling on Windows and X11; the window's desktop scaling (1) on macOS, where windows are placed in points
    In the examples, areas are x,y width×height in pixels and picker sizes are in window units; row heights per look are Linux 64, Mac 64, Windows 62.
    examples
      MaxListHeight(0,0 1280×800, 1, 64) => 448; seven whole rows of 0.62 × 800 = 496
      MaxListHeight(0,0 1920×1040, 1, 62) => 620
      MaxListHeight(0,0 1280×480, 1, 64) => 256
      MaxListHeight(0,0 2560×1600, 2, 64) => 448; the area in window units is 1280×800
      MaxListHeight(0,0 1280×60, 1, 64) => 64; never less than one row
      MaxListHeight(no screen) => 600
      FitsOnScreen(715, 0,0 1280×800, 1) => true
      FitsOnScreen(820, 0,0 1280×800, 1) => false; the preview is then left out and the picker laid out again
      FitsOnScreen(500, 0,0 2560×1600, 2) => true; 1000 pixels
      Position(0,0 1280×800, 756×584, 1) => 262,128
      Position(0,0 1280×800, 756×736, 1) => 262,64
      Position(0,0 1280×800, 756×900, 1) => 262,0; never above the top
      Position(1920,0 1280×1024, 756×584, 1) => 2182,163
      Position(0,0 600×800, 756×584, 1) => 0,128; never left of the area
      Position(0,0 2560×1600, 756×584, 2) => 524,256
      FindScreen(two screens, pointer on the second) => the second
      FindScreen(two screens, pointer nothing) => the primary screen, else the first
      FindScreen(macOS, pointer in points 1500,300 over a Retina screen 0,0 3024×1964 at scaling 2) => that screen; the pointer is matched against the bounds divided by the scaling
  how
    decide the picker is placed after it is shown, from its laid-out size, and placed again whenever its content changes
      because the window sizes itself to its content, which is only known once it exists (PickerPresenter.ShowAsync)
    decide ShowAsync first asks for the preview, lays out, and drops the preview only if FitsOnScreen is false
      because the taller picker with its preview should be the normal case (PR #634)
    decide the history's height is whole rows, the look's row height read from its style ("PickerRowHeight")
      because the last visible row is never cut in half
    decide coordinates are truncated to whole pixels
      because window positions are whole pixels
  sources
    src/Shapeshifter.Desktop/Services/PickerPresenter.cs: TopFraction, HeightFraction, SetMaxListHeight, FitsOnScreen, Position, GetScaling, FindScreen
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml, MacOS.axaml: PickerRowHeight
    issues #143, #211, #280; PR #634
```
