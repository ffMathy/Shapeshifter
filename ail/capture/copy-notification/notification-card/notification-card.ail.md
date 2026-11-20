# The notification card

The small window that shows the "Copied" notification and the tips, styled as
each system's own notifications.

```ail
node NotificationCard : view   level: experience
  why   It should look like the system's own notifications, so it reads as
        part of the desktop rather than an interruption.   <- CopyNotification
  what
    Scenario: a copy on Linux
      When something new is copied
      Then a dark, rounded pill with one line of white text appears at the bottom centre, 8 points above the usable screen's bottom edge
    Scenario: a tip on Linux
      When a tip is shown
      Then the same pill grows to a bold title over a wrapped sentence, at most 420 points wide
  look
    every look      a window of its own: no frame, always on top, never focused, not in the taskbar, sized to its content
    Linux           like a GNOME toast: dark pill #2E2E33, corner radius 21, at least 42 tall, 20 points of padding left and right, 14-point white text in Adwaita Sans, one line trimmed with "…" at 420 points; no icon, no app name, no preview of the item
    Linux, no compositor  the pill fills its window, with square corners and no shadow (as on the test desktop)
    Linux tip       title bold; message 14-point white at 87 % opacity, wrapped at 420 points
    Windows         Fluent card, 8 corner radius, Mica or Acrylic behind it, 330 wide: a 16-point app icon and "Shapeshifter" at 12 points, the title at 14 points semibold, the preview at 12 points in up to three lines (a tip's message at 13 points, up to four lines)
    Mac             the Refraction look's card, 340 wide with corner radius 22, on the system's frosted material, top right under the menu bar
    placement       Linux bottom centre, Windows bottom right, Mac top right; 8 points from the usable screen's edges, plus the look's shadow margin
    right to left   the card mirrors for Arabic and Persian
  copy
    app     "Shapeshifter"            next to the icon, where the look shows it
    title   "Copied · {count} items in your history"   or a tip's title
    preview the copied item's one-line preview: its text, "Image", the file's name or "{count} files"
  sources
    src/Shapeshifter.Desktop/Views/ToastWindow.axaml
    src/Shapeshifter.Desktop/Styles/Linux.axaml, Windows.axaml and MacOS.axaml, the toast styles and ToastPlacement
    src/Shapeshifter.Desktop/Services/CopyNotifier.cs, Place
    assets/screenshots/toast-windows.PNG, toast-macos.PNG, toast-linux.PNG
    driven on Linux (Xvfb, openbox) 2026-11-20: screenshots of the copied pill and of the "Did you know?" tip
```
