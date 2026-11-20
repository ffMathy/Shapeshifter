# The "Copied" notification

Each new copy shows a small notification for a moment: "Copied · 5 items in
your history". It reminds the user that the history is there, and gets out of
the way at once.

```ail
node CopyNotification : flow   level: experience
  why   The history stays out of sight until Ctrl+V is held, so a glance at
        each copy reminds the user it is there and counting.   <- Capture
  what
    Scenario: a copy shows the Copied notification at the bottom centre for a moment
      When something new is copied in another app
      Then within a moment a one-line notification appears, centred, just above the bottom of the screen and any panel there
      And it reads "Copied · N items in your history", N counting every item, pinned ones too
      And about a second and a half later it is gone
    Scenario: copying the same thing twice in a row shows it only once
      Given "twice" was copied and its notification has gone
      When "twice" is copied again
      Then no notification appears
    Scenario: pointing at the notification makes it go away
      Given a notification is on screen
      When the user moves the pointer onto it
      Then it is gone at once
    Scenario: with Copy notifications off, a copy shows nothing
      Given "Copy notifications" is turned off in the settings
      When something new is copied
      Then no notification appears
      When "Copy notifications" is turned back on and something new is copied
      Then the notification appears again
  how
    decide only content new to the history is announced; a copy that only moves an item to the top shows nothing
      because "unknown, confirm with the owner"
    decide the notification never takes the keyboard focus
      because the first notification stole the focus from the app being typed in (#574)
    decide it shows for 1.5 s, and pointing at it or clicking it dismisses it at once
      because a notification over something the user needs must not make them wait (#340)
      because "unknown, confirm with the owner: why 1.5 s"
    decide it sits where the system's own notifications appear: bottom centre on Linux, bottom right on Windows, top right on a Mac
      because a notification in an odd corner covered other apps' notifications (#340; commit 2639a53)
    decide "Copy notifications" can turn it off; tips still show
      because some users find the counter a distraction (#400)
      because "unknown, confirm with the owner: should Copy notifications off also hide tips?"
    decide a new copy while one is shown replaces its text and starts its time again
      because "unknown, confirm with the owner"
  copy
    toast.one               "Copied · 1 item in your history"
    toast.other             "Copied · {count} items in your history"
    settings.notifications  "Copy notifications"
    settings.notifications.detail  "Show a notification each time something is copied"
  children  NotificationCard
  sources
    GitHub issues #340 "Charm position and right click to dismiss", #400 "Quiet mode", #574 "Copying charm notification steals focus", #525 (an old per-window counter)
    PR #554 (QuietMode); commits 57143ed, 2639a53
    driven on Linux (Xvfb, openbox, an SNI tray) 2026-11-20: screenshot of "Copied · 3 items in your history" (a 246 × 42 dark pill at 517,720 over a 1280 × 770 work area); pointer moved onto it; the switch turned off and on
    src/Shapeshifter.Desktop/Services/CopyNotifier.cs; src/Shapeshifter.Desktop/Views/ToastWindow.axaml and ToastWindow.axaml.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, OnCaptured
```
