# The system's material

Windows 11 and macOS only; nothing here can be driven on Linux, where the
picker always uses its look's own panel.

```ail
node NativeMaterial : fn   level: system
  why   A drawn panel is only an imitation of the system's frosted material;
        where the system offers it, the picker should sit on the real thing,
        with the system's corners and shadow (PRs #625, #626).   <- Looks
  what
    Prepare(window: Window, panel: Border) -> nothing   — before the window is first shown
    ClassName: text = "nativeMaterial"   — set on the panel while the material is on
    Variable: text = "SHAPESHIFTER_NATIVE_MATERIAL"   — an environment variable whose value is "off" | "glass" | "acrylic" | "emulated"
    examples
      Prepare(picker, look Windows, Windows 11, transparency effects on) => Mica (Avalonia's), else Acrylic; a thin frame for DWM's 8 px corners and shadow; panel gets "nativeMaterial"
      Prepare(picker, look Windows, SHAPESHIFTER_NATIVE_MATERIAL "acrylic") => Acrylic
      Prepare(picker, look Windows, high contrast on) => nothing; the look's opaque panel
      Prepare(picker, look Windows, Windows 10) => nothing; the look's panel and drawn shadow
      Prepare(picker, look Mac, on macOS) => the always-active popover material behind the sheet, with the system shadow
      Prepare(picker, look Mac, on macOS 26, SHAPESHIFTER_NATIVE_MATERIAL "glass") => Liquid Glass
      Prepare(picker, SHAPESHIFTER_NATIVE_MATERIAL "off") => nothing
      Prepare(picker, look Linux) => nothing
      Prepare(picker, look Windows, on Linux) => nothing; the look is not on its own system
      material on, panel styles => no room around the panel and no drawn shadow: Windows the flyout's Acrylic tint #D9F9F9F9 (dark #E62C2C2C) or transparent on Mica; Mac a fill from white 40 % to #FBFBFD at 25 %
      material refused after showing => the class removed again; the look's own panel and shadow
  how
    decide the window is exactly the panel while the material is on
      because both systems put the material under the whole window; a transparent margin for a shadow would show as a frosted box (NativeMaterial)
    decide on Windows, Avalonia's Mica for the picker rather than the system's
      because the system's goes opaque while the window is inactive, and the picker never is active (docs/architecture.md)
    decide "Transparency effects" off and high contrast give the opaque panel
      because the look's colours are not high-contrast colours, and Windows draws no material then (docs/architecture.md)
      because "unknown, confirm with the owner: the looks have no high-contrast colours yet; is that planned?"
    decide battery and energy saver show Avalonia's Acrylic as an untinted blur under the look's tint
      because it is a known limit: they turn transparency off without changing the setting (docs/architecture.md)
  sources
    src/Shapeshifter.Desktop/Services/NativeMaterial.cs
    src/Shapeshifter.Platform/Windows WindowsWindowMaterial, src/Shapeshifter.Platform/MacOS MacWindowMaterial
    src/Shapeshifter.Desktop/Styles/Windows.axaml and MacOS.axaml, "nativeMaterial"
    docs/architecture.md, "Looks"; PRs #625, #626
    not driven: Windows and macOS only
```
