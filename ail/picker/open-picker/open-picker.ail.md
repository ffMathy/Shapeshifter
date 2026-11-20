# Where the picker opens

```ail
node OpenPicker : flow   level: experience
  why   The picker must appear where the user looks, on the screen they work on,
        entirely on that screen, and must never take the keyboard away from the
        app the paste is for (#143, #211, #280, #639).   <- Picker
  what
    the picker appears on the screen that has the pointer, centred across it, its top edge 16 % of the way down
    a picker too tall for that moves up until it ends at the bottom of the screen, and never above the top
    "the screen" here is its working area: the screen less the panels and docks the desktop keeps on it
    the picker never takes the keyboard focus; only a search gives it the focus, until it closes
    Scenario: the picker opens centred near the top of the screen
      Given "centred" and a unique word were copied, and the picker was shown once before for the same history
      When the user holds Ctrl+V until the picker appears
      Then the picker is centred across the screen, with its top edge 16 % of the way down
    Scenario: the picker never takes the keyboard focus
      Given a text box in another app has the focus
      When the user holds Ctrl+V until the picker appears
      Then the other app still has the focus
      And it still has it after Escape closes the picker
    Scenario: a picker too tall for its usual place moves up to end at the bottom of the screen
      Given a 640 × 400 image was copied, so the picker with its preview is taller than 84 % of the screen, and it was shown once before
      When the user holds Ctrl+V until the picker appears
      Then the picker's bottom edge is the screen's bottom edge, and it is still centred across the screen
    Scenario: the picker is placed for the size it had the time before
      Given the picker was last shown tall, with an image chosen
      And then a short text was copied
      When the user holds Ctrl+V until the picker appears
      Then the shorter picker opens where the tall one was placed, higher than 16 % down
    Scenario: on a screen too short for the picker, it is shown in turn whole without its preview and cut off with it
      Given a screen 480 pixels high, and a text of three lines was copied
      When the user holds Ctrl+V until the picker appears, four times
      Then the first and third times the picker keeps its preview, "51 characters", and its footer is below the screen's edge
      And the second and fourth times it has no preview and shows whole, its footer with "Esc" on screen
  how
    decide centred across the screen, near its top
      because "unknown, confirm with the owner: v6 opened at the top left (#224 asked for the right); why centred, 16 % down?"
    decide on the screen that has the pointer, else the primary screen
      because the user works where the pointer is (#143), and the old placement put the list off screen on some monitor layouts (#211, #280)
    decide the picker is shown without being activated, and is always on top
      because the paste must land in the app the user was in; other clipboard managers steal the focus (#639, PR #614)
    decide the preview is left out when the whole picker is taller than the screen's working area
      because the picker must stay on one screen with all its keys visible (PR #634)
    decide the place is worked out from the window's size as last laid out by the system
      because "unknown, confirm with the owner: keep this? On Linux the picker is placed for its previous size, so it can reach below the screen, and on a short screen the preview is left out only every other time"
  uses      PlacementRules
  children  PlacementRules
  sources
    issues #143, #211, #224, #280, #639; PRs #614, #618, #634
    docs/architecture.md, "The picker"
    driven on Linux (Xvfb, openbox) 2026-11-20 on 1280 × 800: a text picker of 584 at 128 (16 %); an image picker of 736 at 64 the second time, at 128 (reaching to 864) the first time after a text; a text picker of 605 at 64 after the image picker; on 1280 × 600 it fit; on 1280 × 480 it alternated: cut off with its preview, then whole without it
    driven 2026-11-20: Xvfb's two-screen mode reported both screens at 0,0, so the pointer's screen could not be driven
    src/Shapeshifter.Desktop/Services/PickerPresenter.cs, ShowAsync, FitsOnScreen, Position, FindScreen
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml: ShowActivated False, Topmost True, Focusable False, SizeToContent
```
