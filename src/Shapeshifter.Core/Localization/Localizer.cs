using System.Globalization;

namespace Shapeshifter.Core.Localization;

/// <summary>
/// The language Shapeshifter speaks. Text comes from <see cref="Strings"/> (Localization/Strings.resx and its
/// translations), in the operating system's display language unless <see cref="Variable"/> chooses another one.
/// </summary>
public static class Localizer
{
    /// <summary>Overrides the display language with a culture name such as <c>de</c>, <c>pt-BR</c> or <c>en</c>.</summary>
    public const string Variable = "SHAPESHIFTER_LANGUAGE";

    /// <summary>The languages Shapeshifter is translated into, besides English (the neutral resources).</summary>
    public static IReadOnlyList<string> Translations { get; } =
    [
        "ar", "bn", "da", "de", "es", "fa", "fr", "hi", "id", "it", "ja",
        "ko", "nl", "pl", "pt-BR", "ru", "th", "tr", "uk", "vi", "zh-Hans",
    ];

    /// <summary>Applies <see cref="Variable"/>, when it is set to a known culture, to every thread of the process.</summary>
    public static void ApplyLanguageOverride()
    {
        var name = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(name.Trim());
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // An unknown name leaves the system language in place.
        }
    }

    /// <summary>Whether the current display language is written from right to left (Arabic, Persian).</summary>
    public static bool IsRightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    /// <summary>Fills in a translated format string with numbers and dates in the current culture.</summary>
    public static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    /// <summary>A string of <see cref="Strings"/> in <paramref name="culture"/>, falling back to English.</summary>
    public static string Get(string name, CultureInfo culture) =>
        Strings.ResourceManager.GetString(name, culture) ?? name;

    /// <summary>The key cap of the modifier key held for the picker: "Ctrl" in the local spelling ("Strg"), "Cmd" as is.</summary>
    public static string ModifierKey(string modifierName) =>
        modifierName == "Ctrl" ? Strings.KeyControl : modifierName;
}
