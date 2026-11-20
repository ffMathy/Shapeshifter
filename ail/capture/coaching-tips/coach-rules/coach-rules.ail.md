# Coach rules

The coach as plain code: copies, quick pastes and uses of the picker in; at
most one tip out per copy.

```ail
node CoachRules : fn   level: system
  why   Whether a tip shows depends on timing, app names and what came
        before, so the rules must be exact and testable with a fake clock.   <- CoachingTips
  what
    CopyCoach(sourceLocator: IClipboardSourceLocator or nothing, modifierName: () -> text, isEnabled: (() -> boolean) or nothing = nothing, time: TimeProvider or nothing = nothing) -> CopyCoach
    CopyCoach.OnStartupCapture(item: ClipboardItem) -> nothing
    CopyCoach.OnCopied(item: ClipboardItem, isNew: boolean, source: ClipboardSource or nothing) -> CoachingTip or nothing
    CopyCoach.OnPasted() -> nothing
    CopyCoach.OnPickerUsed() -> nothing
    type CoachingTip = { Kind: "CopiedAgain" | "CopiedBackAndForth", Title: text, Message: text }
    Cooldown: duration = 5 min
    BackAndForthWindow: duration = 30 s
    In the examples, copy(x, new|old, App, t) is OnCopied at t seconds from source App, and paste(App, t) is OnPasted with the locator answering App.
    examples
      OnStartupCapture(a); copy(a, old, Editor, 1) => nothing       a is what the clipboard already held
      copy(a, new, Editor, 0); copy(a, old, Editor, 1) => nothing   a second Ctrl+C to be sure
      copy(a, new, Editor, 0); copy(b, new, Editor, 5); copy(a, old, Editor, 9) => CopiedAgain("Already in your history", "No need to copy it again: hold Ctrl+V to pick it from your history.")
      copy(x, new, Editor, 0); paste(Browser, 5); copy(y, new, Editor, 30) => CopiedBackAndForth("Did you know?", "You can copy several things from Editor first, then hold Ctrl+V in Browser to pick each one from your history.")
      copy(x, new, Editor, 0); paste(Browser, 5); copy(y, new, Editor, 31) => nothing             more than 30 s after the first copy
      copy(x, new, Editor, 0); paste(editor, 5); copy(y, new, Editor, 9) => nothing               same app: names compare ignoring case
      copy(x, new, Editor, 0); paste(Browser, 5); copy(y, new, Mail, 9) => nothing
      copy(x, new, Editor, 0); paste(Browser, 5); OnPickerUsed(); copy(y, new, Editor, 9) => nothing
      copy(x, new, Editor, 0); paste(Browser, 5); copy(y, new, Mail, 7); copy(z, new, Editor, 9) => nothing   any copy ends the trip
      copy(x, new, unknown app, 0); paste(Browser, 5); copy(y, new, Editor, 9) => nothing
      both habits at once => CopiedAgain                     it is checked first
      a tip at t; another habit at t + 299 s => nothing;  at t + 300 s => a tip
      isEnabled() false => nothing, and the cooldown does not start
      modifierName() "Cmd" => the messages say "Cmd+V"; read when the tip is made
    In the engine, a tip replaces the "Copied" notification: NotifyTip(tip) and no NotifyCopied; when a tip is suppressed, a new item still gets NotifyCopied
    In the engine, PasteCurrentClipboard calls OnPasted() before the keystroke, and ShowPicker or PerformAction call OnPickerUsed().
  how
    decide app names are trimmed; an empty or blank name counts as unknown
      because "unknown, confirm with the owner"
    decide the paste target is located when the quick paste runs, outside the coach's lock
      because locating queries the window system, and the coach is called from both the capture thread and the effect loop
    decide a locator that throws is treated as no target
      because a tip is never worth failing a paste for (CopyCoach)
  sources
    src/Shapeshifter.Core/Coaching/CopyCoach.cs
    src/Shapeshifter.Core/Engine/ShapeshifterEngine.cs, ProcessAsync and OnCaptured
    src/Shapeshifter.Desktop/App.axaml.cs (CopyCoach wired to settings.Current.CoachingTips and the keyboard's modifier name)
    src/Shapeshifter.Core/Localization/Strings.resx, TipAlreadyInHistoryTitle, TipAlreadyInHistoryMessage, TipDidYouKnowTitle, TipCopyBackAndForthMessage
    PR #631; commit 4d44595
```
