# Changing a setting

What each control in the settings window does, the moment it is changed.

```ail
node ChangeSettings : flow   level: experience
  why   A setting is only worth having if changing it is felt straight away, without
        a restart or a Save button.   <- SettingsWindow
  what
    Scenario: a longer delay keeps the history hidden for longer
      Given the user types 1500 as the delay before the history appears
      When the user holds Ctrl and V for one second
      Then the history has not appeared yet
      When the user keeps holding them for another half second
      Then the history appears
    Scenario: turning off copy notifications hides the Copied notification
      Given the user turns off "Copy notifications"
      When the user copies something
      Then no notification appears
      When the user turns "Copy notifications" back on and copies something else
      Then the "Copied" notification appears
    Scenario: Items to remember limits how much is kept
      Given the user types 1 as the number of items to remember
      And "dropped" and then "kept" were copied
      When the user holds Ctrl and V until the history appears, presses V again and lets go of Ctrl
      Then "kept" is pasted, not "dropped", which is no longer in the history
    Scenario: Clear history forgets older copies but keeps the clipboard
      Given "before" and then "current" were copied
      When the user presses "Clear history" in the settings
      And holds Ctrl and V until the history appears, presses V again and lets go of Ctrl
      Then "current", still on the clipboard, is pasted, not "before"
    Scenario: Copy version puts the version and the system on the clipboard
      When the user clicks "Copy version"
      Then the clipboard holds the name, the version, the system and the processor, with X11 or Wayland on Linux,
        joined by " · ", for example "Shapeshifter 7.1.0 · Ubuntu 24.04.5 LTS · X64 · X11"
    Scenario: Report a problem opens a new GitHub issue
      When the user clicks "Report a problem"
      Then the browser opens the same new issue as the tray menu's "Report a problem…"
    Scenario: Project page opens the project on GitHub
      When the user clicks "Project page"
      Then the browser opens the project's GitHub page
    Scenario: Check for updates opens the releases page when this copy cannot update itself
      Given this copy cannot update itself, as a build from source cannot
      When the user presses "Check for updates"
      Then the browser opens the project's releases page
    Scenario: Open log folder opens the folder of the logs
      When the user presses "Open log folder"
      Then the file manager opens the folder that holds the logs
    Lowering the number of items to remember drops the oldest unpinned items at once; pinned items never count.
    "Remember history after restart" on keeps the unpinned history on disk, so it is back after a restart;
      turning it off deletes what was kept. Pinned items are always kept.
    "Tips" off stops the tips shown when the history could have saved a copy.
    "Start at login" on adds an entry that starts this copy at sign-in; off removes it.
    "Automatic updates" on checks for an update straight away when this copy can update itself.
    "Clear history" removes every unpinned item; the clipboard keeps what it holds.
  how
    decide the delay goes from 0 to 3000 ms in steps of 50, and items to remember from 1 to 1000 in steps of 5
      because "unknown, confirm with the owner: why these limits and steps"
    decide copy notifications are on by default, and can be silenced
      because silencing them came from a contributor as an opt-in "quiet mode" (PR #554)
    decide the switch shows "Copy notifications" on, the opposite of the saved quiet mode
      because "unknown, confirm with the owner"
    decide remembering the history after a restart is off by default
      because by default nothing but pinned items is written to disk; people choose the tool for that (#459)
    decide a check-for-updates press in a copy that cannot update itself opens the releases page instead
      because a build from source or an unwritable folder must be updated the way it was installed
    decide "Start at login" acts on the entry itself and is not a saved setting
      because "unknown, confirm with the owner: the switch reads the entry each time the window opens"
  uses      Preferences, ProblemReport, StartAtLogin, AutoUpdate
  sources
    issues #154, #459, #577; PR #554 "QuietMode"; commits b3f57d8, 4d44595, 6783ee8
    driven on Linux (Xvfb, openbox, xfce4-panel tray) 2026-11-20: each scenario; "Start at login" on and off wrote and removed the autostart entry
    src/Shapeshifter.Desktop/ViewModels/SettingsViewModel.cs, the On…Changed handlers and the commands
    src/Shapeshifter.Desktop/App.axaml.cs, the settings' Changed handler in StartAsync
    src/Shapeshifter.Desktop/Views/SettingsWindow.axaml, Minimum, Maximum and Increment of the number boxes
```
