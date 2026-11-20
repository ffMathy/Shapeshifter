# Accent shades

```ail
node AccentShades : fn   level: system
  why   The system's accent colour can be any colour; text and marks drawn in it
        must stay readable on light and dark surfaces (#164; commit fd387a4
        "Give the looks softer and text-safe accent shades").   <- Looks
  what
    Attach(application: Application) -> nothing — applies the platform's accent, and again whenever the system's colours change
    Apply(application: Application, accent: Color) -> nothing
    LightThemeAccentText(accent: Color) -> Color
    DarkThemeAccentText(accent: Color, light2: Color or nothing) -> Color
    ContrastRatio(a: Color, b: Color) -> number — WCAG 2 relative luminance, (lighter + 0.05) / (darker + 0.05)
    Darken(color: Color, factor: number) -> Color — each channel × factor, rounded
    resources set by Apply, the accent's alpha ignored
      AccentBrush = accent;  AccentColor = accent
      AccentDarkBrush = Darken(accent, 0.7);  AccentDarkerBrush = Darken(accent, 0.6)
      ActionsBackgroundBrush = AccentDarkBrush at alpha F0
      AccentSoftBrush = accent at alpha 24 (14 %);  AccentFaintBrush = accent at alpha 14 (8 %)
      AccentTextBrush, AccentTextColor: in the light (Default) theme dictionary LightThemeAccentText(accent); in the Dark one DarkThemeAccentText(accent, Windows' Light 2)
    examples
      LightThemeAccentText(#0067C0) => #0067C0; already 5.7:1 on white
      LightThemeAccentText(#0078D7) => #006EC6; 4.497:1, darkened once by 0.92 (seen on Linux: the toolkit's default accent)
      LightThemeAccentText(#3584E4) => #2D6FC1; darkened twice
      LightThemeAccentText(#3A5CF0) => #3A5CF0
      LightThemeAccentText(#FFFFFF) => darkened by 0.92 again and again until 4.5:1 on white
      Darken(#0078D7, 0.7) => #005496; 215 × 0.7 = 150.5 rounds to the even 150
      Apply(accent #3A5CF0 at alpha 80) => AccentBrush #3A5CF0
      Attach(platform reporting an accent with alpha 0) => nothing set; the look's own palette stays
      DarkThemeAccentText(accent, light2 #99EBFF) => #99EBFF, mixed 8 % toward white per step while under 4.5:1 on #2C2C2C or under 3:1 on #575757
      DarkThemeAccentText(accent, nothing) => the accent 70/255 lighter in HSL, then mixed toward white the same way
  how
    decide the text shade goes in the theme dictionaries, never at the top level
      because a top-level entry would win over every theme dictionary (AccentPalette)
    decide light: darken until 4.5:1 on white, as Windows does in light mode
      because the accent is used for text and strokes on white (AccentPalette)
    decide dark: Windows' own "Light 2" shade when Windows computed it for this same accent, else an approximation
      because the Settings app uses that shade in dark mode (PR #625)
    decide the darkest dark surface is #2C2C2C and the lightest #575757
      because those are Fluent's dark flyout tint and the chosen action over a white desktop (AccentPalette)
  sources
    src/Shapeshifter.Desktop/Services/AccentPalette.cs
    src/Shapeshifter.Platform/Windows, WindowsAccentPalette (Explorer's AccentPalette)
    issue #164; PR #625; commit fd387a4
    driven on Linux 2026-11-20: the picker's accent sampled at 0,110,198 and its tint at 219,236,250
```
