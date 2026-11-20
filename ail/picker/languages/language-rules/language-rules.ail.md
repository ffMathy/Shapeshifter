# Language rules

```ail
node LanguageRules : fn   level: system
  why   Which language is spoken, and which way the windows read, must be decided
        once, before any window exists, the same way for every window.   <- Languages
  what
    Variable: text = "SHAPESHIFTER_LANGUAGE"
    Translations: list of text = ["ar", "bn", "da", "de", "es", "fa", "fr", "hi", "id", "it", "ja", "ko", "nl", "pl", "pt-BR", "ru", "th", "tr", "uk", "vi", "zh-Hans"]
    ApplyLanguageOverride() -> nothing — first thing at start: sets the display (UI) culture of every thread to the variable's culture
    IsRightToLeft: boolean — the display culture's text info is right to left
    Format(format: text, args) -> text — the text filled in with the current (format) culture
    Get(name: text, culture: Culture) -> text — that culture's text, else English, else the name itself
    ModifierKey(name: text) -> text — the translated "Ctrl" for "Ctrl", the name as is otherwise
    examples
      ApplyLanguageOverride(variable "de") => display culture de; strings German
      ApplyLanguageOverride(variable " pt-BR ") => display culture pt-BR; trimmed
      ApplyLanguageOverride(variable unset or blank) => the system's display language
      ApplyLanguageOverride(variable "!!") => the system's display language; an unknown name is ignored
      ApplyLanguageOverride(variable "sv") => display culture sv; strings English, the fallback
      IsRightToLeft(display culture ar) => true;  (fa) => true;  (de) => false
      IsRightToLeft(display culture he) => true; strings English, mirrored
      ModifierKey("Ctrl", display culture de) => "Strg";  ModifierKey("Cmd") => "Cmd";  ModifierKey("Win") => "Win"
      Get("TimeJustNow", culture de) => "Gerade eben";  Get("TimeJustNow", culture sv) => "Just now"
      Format("{0} items", 3) => "3 items"
      the picker's footer key cap for Cmd => "⌘" in every language
    only the display culture changes; numbers in Format follow the system's format culture, dates in RelativeTime the display culture
    a right-to-left display culture sets the picker's, the settings window's and the notification's flow direction when they are created
  how
    decide the override is read once, before the first window
      because every string is read through the display culture, and the windows take their direction when they are made (Program.Main)
    decide a language without a translation keeps its culture but shows English
      because the resources fall back to the neutral English table (Strings.resx)
    decide Hebrew and other right-to-left languages without a translation are mirrored all the same
      because "unknown, confirm with the owner: keep this? English text then reads mirrored"
  sources
    src/Shapeshifter.Core/Localization/Localizer.cs
    src/Shapeshifter.Desktop/Program.cs, ApplyLanguageOverride called first
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml.cs, the flow direction
    src/Shapeshifter.Desktop/ViewModels/PickerViewModel.cs, SetModifierName
    PR #633
```
