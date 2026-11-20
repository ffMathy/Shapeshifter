# The settings window

One window with every setting, how to use the gesture, the version, and links
to report a problem.

```ail
node SettingsWindow : view   level: experience
  why   People need one place that explains the gesture and holds every choice,
        and that names the version when something goes wrong.   <- App
  what
    It opens from the tray icon, from "Settings…" and "About" in the tray menu, by itself at start when the
      platform limits Shapeshifter, and on "Check for updates…" in a copy that can update itself.
    Only one settings window is ever open; opening it again brings it forward.
    Every change is saved and takes effect as it is made; there is no Save or OK button.
    The usage text and the delay's description name the gesture's key, and follow the Shortcut setting at once.
    Scenario: the settings window shows every setting with its current value
      Given the settings were never changed, on Linux
      When the settings window opens
      Then it shows the delay 300, items to remember 50, "Remember history after restart" off,
        copy notifications on, tips on, start at login as it is, and automatic updates on
      And there is no "Shortcut" row, as Linux offers no other shortcut
  look
    window      titled with the "title" copy, 540 wide, as tall as its content, not resizable, centred on the screen
    hero        the app icon, the app's name in bold and under it the version with three links: copy version,
                report a problem, project page
    usage       the usage text, centred, in dim text
    warning     only when the platform limits the app: a yellow card with each limit, a blank line between them
    groups      a "History" heading over a card of rows: Shortcut (Windows and Mac only), the delay, items to remember,
                remember history; a "Behaviour" heading over: copy notifications, tips, start at login, automatic updates
    row         a title, a smaller dim description under it, and its control on the right: a number box with − and +
                buttons, a switch or a drop-down; rows are split by hairlines
    buttons     a wrapping row of rounded buttons with an icon: clear history, check for updates, open log folder
    disabled    the start-at-login switch is greyed when the platform has none; the updates switch is greyed, still showing
                its saved state, when this copy cannot update itself, and its description then says why
    each look   Windows, Mac and Linux style the same layout their own way (on Linux: the GNOME settings dialog, no row icons)
  copy
    title                "Shapeshifter Settings"
    version              "Version {version}"
    copyVersion          "Copy version"           reads "Copied" for 2 seconds after a click
    report               "Report a problem"
    project              "Project page"
    usage                "Copy as usual. To paste an older item, hold {key}+V until the history appears, then keep {key} held: press V again (or ↑/↓, or 1–9) to pick an item, → to choose what to do with it, Delete to remove it and Esc to cancel. Release {key} to paste. To look through the history without holding {key}, press Space: it stays open until you press Enter to paste or Esc."
    group.history        "History"
    shortcut             "Shortcut"   with "Win+V replaces the Windows clipboard history, so Ctrl+V pastes as usual" on Windows, "Ctrl+V leaves Cmd+V free to paste as usual" on a Mac
    delay                "Delay before the history appears"   with "How long to hold {key}+V before the history appears"
    items                "Items to remember"                   with "Pinned items are kept in addition"
    remember             "Remember history after restart"      with "Keeps the history on this computer's disk, not just pinned items"
    group.behaviour      "Behaviour"
    notifications        "Copy notifications"                  with "Show a notification each time something is copied"
    tips                 "Tips"                                with "Suggest when the history could have saved you a copy"
    login                "Start at login"                      with "Shapeshifter waits in the background after you sign in"
    updates              "Automatic updates"                   with the update status, see AutoUpdate
    clear                "Clear history"
    check                "Check for updates"
    logs                 "Open log folder"
  how
    decide the version and the "Copy version" link sit at the top of the settings
      because people could not find the version to report a bug (#342)
    decide the window is as tall as its content, with no scrolling
      because "unknown, confirm with the owner: keep this? on a screen lower than about 860 pixels (driven at 1280×800) the bottom row of buttons is cut off and 'Open log folder' cannot be clicked"
    decide the buttons wrap onto a second row
      because translated labels can be much longer than the English ones
    decide Esc does not close the window; the window manager's close does
      because "unknown, confirm with the owner: keep this?"
  uses      ChangeSettings, Preferences, ShortcutChoice, AutoUpdate, PlatformRequirements, ProblemReport
  children  ChangeSettings, Preferences, ShortcutChoice
  sources
    issue #342; commits 741b0e5, 8bded19, b3f57d8, 4d44595
    driven on Linux (Xvfb 1280×800, openbox) 2026-11-20: screenshots of the window, its lower half, and the Wayland warning card
    src/Shapeshifter.Desktop/Views/SettingsWindow.axaml and SettingsWindow.axaml.cs
    src/Shapeshifter.Desktop/ViewModels/SettingsViewModel.cs
    src/Shapeshifter.Desktop/Styles/Linux.axaml, the settings part ("Boxed Sheet")
    src/Shapeshifter.Core/Localization/Strings.resx, Settings*
```
