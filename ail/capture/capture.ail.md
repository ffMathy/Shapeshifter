# Capturing copies

Everything copied in any app joins the history, with nothing to press: the
newest on top, the same thing never twice, and secrets left out.

```ail
node Capture : flow   level: experience
  why   The history is only worth holding Ctrl+V for if everything the user
        copied is in it, with nothing to do at copy time.   <- Shapeshifter
  what
    Scenario: each copy joins the history, newest first
      Given "older" and then "newer" were copied in another app
      When the user picks the second item from the history and pastes it
      Then "older" is pasted
    Scenario: copying something again moves it to the top instead of adding it twice
      Given "before", "again", "between" and "again" were copied, in that order
      Then the history starts "again", "between", "before"
      And "again" is in it only once
    Scenario: a password manager's secret is not recorded
      Given "plain" was copied
      When a password manager copies "secret", marked as a secret
      Then no notification appears
      And the newest item in the history is still "plain"
    Scenario: an app that writes the clipboard several times at once adds one item
      Given "steady" was copied
      When an app puts "draft" and then "final" on the clipboard a moment apart
      Then the history starts "final", "steady"
    Scenario: pasting from the history adds nothing to it
      Given "picked" and then "last" were copied
      When the user picks "picked" from the history and pastes it
      Then no notification appears
      And the history starts "picked", "last"
  how
    decide every copy is captured, whatever app it came from, with no shortcut to press
      because a clipboard manager that has to be told to save something is no help when the user forgot to (README, "Copy many things")
    decide the same content moves to the top and gets a new copy time, rather than appearing twice
      because many duplicates of one selection made v6's history long and confusing (#597, #245)
    decide content that the copying app marks as secret is never read beyond the marker
      because passwords must not end up in a history that stays on screen and may be saved to disk (README, "Passwords are skipped"; commit db7046f)
    decide clipboard changes less than 100 ms apart count as one copy
      because apps often write the clipboard several times for one copy (docs/architecture.md)
      because "unknown, confirm with the owner: why 100 ms"
    decide Shapeshifter's own clipboard writes are not captured as copies
      because a paste from the history would otherwise count as a new copy, show a notification or a tip, and on some systems add a re-encoded image a second time (docs/architecture.md)
    decide what is on the clipboard when Shapeshifter starts is captured too, and gets the "Copied" notification
      because "unknown, confirm with the owner: keep this?"
  copy
    item.image        "Image"
    item.files        "{count} files"
    item.formatted    "Formatted text"     rich text with no plain text that can be shown
  uses      CaptureRules
  children  KeepFormats, HistoryLimit, CopyNotification, CoachingTips, CaptureRules
  sources
    README.md, "Good to know": everything is kept, passwords are skipped
    docs/architecture.md, "Capturing copies"
    GitHub issues #245 "Elliminate duplicates in clipboard list", #597 "Shapeshifter shows many duplicates of a copied selection"
    commit db7046f "Fix defects found in an adversarial review of the port" (password-manager markers)
    driven on Linux (Xvfb, openbox) 2026-11-20: copies from the target app and xclip, picked with the gesture; the target app's copy-secret; two xclip writes 0 ms apart; a paste from the history
    src/Shapeshifter.Core/Clipboard/ClipboardMonitor.cs, src/Shapeshifter.Core/Clipboard/ClipboardHistory.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, StartAsync and OnCaptured
```
