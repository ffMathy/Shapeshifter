# The paste gesture

Ctrl+V, the shortcut everyone already knows, either pastes as usual or opens
the history. Which one depends only on how long V is held.

```ail
node PasteGesture : flow   level: experience
  why   Reach the history with keys people already press dozens of times a day,
        without slowing down the ordinary paste.   <- Shapeshifter
  what
    Scenario: a quick Ctrl+V pastes what was copied last
      Given "alpha" was copied
      And a text box in another app has the focus
      When the user presses Ctrl, taps V and lets go of Ctrl
      Then "alpha" is pasted, once
      And the picker never appears
    Scenario: keeping Ctrl held, each tap of V pastes again
      Given "again" was copied
      When the user holds Ctrl and taps V twice
      Then "again" is pasted after the first tap, and again after the second
      And the picker never appears
      And letting go of Ctrl pastes nothing more
    Scenario: letting go of Ctrl a moment before V still pastes
      Given "rolling" was copied
      When the user presses Ctrl and V, then lets go of Ctrl before V
      Then "rolling" is pasted, once, and no stray "v" is typed
    Scenario: typing V without Ctrl types a v
      When the user taps V twice without Ctrl
      Then the text box shows "vv"
    Scenario: Ctrl+Shift+V is left to the app
      When the user holds Ctrl and Shift and holds V for longer than the show delay
      Then the picker never appears
    Scenario: holding Ctrl+V shows the history
      Given "first" and then "newest" were copied
      When the user holds Ctrl and holds V for the show delay
      Then the picker appears with "newest" chosen
      When the user lets go of V and then of Ctrl
      Then the picker closes and "newest" is pasted, once
  how
    decide a quick press pastes when V comes up, even while Ctrl stays held
      because people paste into one row after another holding Ctrl: Ctrl+V, Down, V, Down, V (#639)
      rejected waiting for Ctrl to come up because it held Ctrl "hostage" and pasted nothing until the end (#639)
      rejected tapping V twice quickly to pick the second item because it cannot coexist with pasting again (PR #643)
    decide the history appears only while V itself is held for the show delay
      because a short press must never flash a window (#7, #154)
    decide when nothing else was chosen, the clipboard is pasted untouched rather than restored from the history
      because a quick paste must be identical to a normal paste, even for data copied milliseconds earlier
    decide Ctrl+V together with Shift, Alt, AltGr or another modifier is left alone
      because Ctrl+Shift+V is paste as plain text or paste in a terminal, and AltGr+V types characters on some layouts (#5)
    decide the show delay is 300 ms by default, and a setting from 0 to 3000 ms
      because people who hold keys long saw the picker flash during ordinary pastes (#154)
      because 300 ms was tuned by feel; long enough never to flash on a normal paste (owner, 2026-11-20)
      because "unknown, confirm with the owner: why at most 3 s"
    decide the picker never takes the keyboard focus
      because the paste must land in the app the user was in; other clipboard managers steal the focus (#639)
  copy
    footer.release    "Release to paste"      next to the "Ctrl" key, while the picker is up
    key.ctrl          "Ctrl"
    title             "Shapeshifter"          the picker's title on Linux
  uses      GestureRules
  children  PickFromHistory, KeepHistoryOpen, GestureRules
  sources
    interview with the owner, 2026-11-20: the show delay
    GitHub issues #1, #5, #7, #154, #527, #639
    PR #643 "Paste on releasing V, so Ctrl can stay held between pastes"
    driven on Linux (Xvfb, openbox) 2026-11-20 with xdotool: quick paste, held Ctrl, Ctrl+Shift+V
    src/Shapeshifter.Core/Input/PasteGestureStateMachine.cs, OnTriggerDown and OnTriggerUp
    src/Shapeshifter.Core/Settings/ShapeshifterSettings.cs, PasteDelayMilliseconds
    docs/architecture.md, "The paste gesture"
```
