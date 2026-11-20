# Languages

```ail
node Languages : flow   level: experience
  why   People should be able to use Shapeshifter in their own language; a
        French translation was the first offer of help (#49), and it now speaks
        the 20 most spoken languages and Danish (PR #633).   <- Picker
  what
    every word Shapeshifter shows is in the display language of the operating system, if it is one of 21 translations: Arabic, Bengali, Danish, German, Spanish, Persian, French, Hindi, Indonesian, Italian, Japanese, Korean, Dutch, Polish, Brazilian Portuguese, Russian, Thai, Turkish, Ukrainian, Vietnamese and Simplified Chinese; any other language gets English
    SHAPESHIFTER_LANGUAGE, set to a language code such as "de" or "pt-BR" before Shapeshifter starts, overrides the system's language
    key caps use the local key names; times, counts and dates are written the local way
    in a right-to-left language (Arabic, Persian) the windows are mirrored: the history on the right, the preview and actions on the left, the footer read from the right
    what was copied is never translated
    Scenario: the picker speaks the language SHAPESHIFTER_LANGUAGE names
      Given Shapeshifter was started with SHAPESHIFTER_LANGUAGE set to "de", and nothing was copied
      When the user holds Ctrl+V until the picker appears
      Then the picker says "Noch nichts kopiert", the modifier's key cap says "Strg", and Escape's hint says "Abbrechen"
      And "Nothing copied yet" is nowhere
    Scenario: the Linux look's list headings stay in English
      Given Shapeshifter was started with SHAPESHIFTER_LANGUAGE set to "de", and "ein kurzer Text" was copied
      When the user holds Ctrl+V until the picker appears
      Then the row says "Gerade eben", yet the headings say "History", "Actions" and "→ to choose"
    Scenario: in Arabic the picker is laid out from right to left
      Given Shapeshifter was started with SHAPESHIFTER_LANGUAGE set to "ar"
      When the user holds Ctrl+V until the picker appears
      Then the footer starts on the right with "Ctrl"
      And in German, by contrast, it starts on the left with "Strg"
  how
    decide one table of every user-facing string, with a translation per language
      because the translations must stay complete as features are added (PR #633: every string, with a note for translators)
    decide the system's display language unless SHAPESHIFTER_LANGUAGE names another
      because "unknown, confirm with the owner: is a language setting in the settings window wanted?" (PR #633 only names the variable)
    decide right-to-left languages mirror the whole layout, icons and arrow key caps included
      because the windows set their flow direction from the language (PR #633)
      because "unknown, confirm with the owner: keep this? in Arabic the caps read 9–1 and ↓↑, and the default action's check mark is mirrored"
    decide the Linux look's "History", "Actions" and "→ to choose", and its title "Shapeshifter", are not translated
      because "unknown, confirm with the owner: keep this? they are fixed in the Linux look; the other looks' titles are translated"
    decide log messages and the problem report stay in English
      because they are read by the developers (PR #633)
  uses      LanguageRules
  children  LanguageRules
  sources
    issue #49 "Translating Shapeshifter to other languages"; PR #633; commits 979824d, f98f85c, fd48b2a
    docs/architecture.md, "Languages"
    driven on Linux (Xvfb, openbox) 2026-11-20 with SHAPESHIFTER_LANGUAGE de and ar, each on a display of its own: German rows, actions and keys ("Einfügen", "Entf", "Schließen", "3 Elemente"); Arabic mirrored with "العناصر: 3", English headings in both
    src/Shapeshifter.Core/Localization/Localizer.cs and Strings.resx with its 21 translations
    src/Shapeshifter.Desktop/Views/PickerWindow.axaml.cs, the flow direction
```
