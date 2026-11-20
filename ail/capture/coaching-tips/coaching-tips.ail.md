# Coaching tips

When the user does something the history could have saved them, a tip takes
the place of the "Copied" notification and says how.

```ail
node CoachingTips : flow   level: experience
  why   Shapeshifter is easy to use, but only once people remember it is
        there; it stays hidden until Ctrl+V is held (#284).   <- Capture
  what
    Scenario: copying the same thing twice in a row shows no tip
      Given "double" was just copied
      When "double" is copied again
      Then neither a tip nor the "Copied" notification appears
    Scenario: with Tips off, copying something again shows nothing
      Given "Tips" is turned off in the settings
      And "a", then "b" were copied
      When "a" is copied again
      Then neither a tip nor the "Copied" notification appears
    Scenario: copying something from the history again shows a tip
      Given no tip was shown in the last five minutes
      And "a", then "b" were copied
      When "a" is copied again
      Then a tip appears in the notification's place: "Already in your history", "No need to copy it again: hold Ctrl+V to pick it from your history."
      And it stays for about seven seconds
    Scenario: a second tip within five minutes is not shown
      Given a tip was shown less than five minutes ago
      When the user copies something from the history again
      Then neither a tip nor the "Copied" notification appears
    Scenario: copying back and forth between two apps shows a tip
      Given no tip was shown in the last five minutes
      And the user copied something in one app and pasted it with a quick Ctrl+V in another
      When, less than 30 seconds after that copy, they copy something new in the first app
      Then a tip appears: "Did you know?", "You can copy several things from {first app} first, then hold Ctrl+V in {other app} to pick each one from your history."
  how
    decide coach two habits: copying again what is already in the history, and copying back and forth between two apps
      because each was a copy the history could have saved (#284, #310)
    decide copying what the clipboard already holds is not coached
      because a second Ctrl+C to be sure is no habit the history could help with (#284, owner's comment)
    decide back and forth counts only within 30 seconds of the first copy, and only after a quick paste into a different app
      because that is the sequence #310 describes: copy in one app, paste in another, back within 30 seconds
    decide holding Ctrl+V until the history appears ends a back-and-forth sequence
      because the user then knows the history is there (CopyCoach)
    decide what is on the clipboard when Shapeshifter starts is never coached
      because "unknown, confirm with the owner" (stated, without a reason, in the closing comment of #284)
    decide tips are at least five minutes apart, and the "Tips" switch turns them off
      because they should remind without nagging (#284, owner's comment)
    decide a tip stays seven seconds instead of 1.5
      because a tip is a sentence to read, not a glance (CopyNotifier)
    decide apps are told apart by name, not by window
      because window titles change as the user works; v6 counted per window, which confused users (#525)
    decide a tip shows even with "Copy notifications" off
      because "unknown, confirm with the owner: keep this?"
  copy
    tip.again.title     "Already in your history"
    tip.again           "No need to copy it again: hold {modifier}+V to pick it from your history."
    tip.trip.title      "Did you know?"
    tip.trip            "You can copy several things from {from} first, then hold {modifier}+V in {to} to pick each one from your history."
    settings.tips         "Tips"
    settings.tips.detail  "Suggest when the history could have saved you a copy"
  uses      CoachRules, CopySource
  children  CoachRules
  sources
    GitHub issues #284 "Coach people in using Shapeshifter when re-copying stuff" (and the owner's closing comment), #310 "More coaching: keep track of windows being copied to/from", #525
    PR #631 "Coaching tips when the history could have saved a copy"; commits 4d44595, 6783ee8
    driven on Linux (Xvfb, openbox, an SNI tray) 2026-11-20: target app plus xlogo; screenshot of "Did you know? You can copy several things from Shapeshifter.EndToEnd.TargetApp first, then hold Ctrl+V in XLogo to pick each one from your history."; a second habit within five minutes showed nothing
    src/Shapeshifter.Core/Coaching/CopyCoach.cs; src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, OnCaptured
```
