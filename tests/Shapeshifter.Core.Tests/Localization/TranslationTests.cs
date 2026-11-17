using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Shapeshifter.Core.Localization;
using Shapeshifter.Core.Text;

namespace Shapeshifter.Core.Tests.Localization;

public sealed partial class TranslationTests
{
    public static TheoryData<string> Translations => new(Localizer.Translations);

    private static Dictionary<string, string> English => Read(CultureInfo.InvariantCulture)!;

    private static Dictionary<string, string>? Read(CultureInfo culture) =>
        Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false) is { } set
            ? set.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!)
            : null;

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    private static string[] Placeholders(string text) =>
        Placeholder().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal).ToArray();

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_HasEveryString(string name)
    {
        var translated = Read(CultureInfo.GetCultureInfo(name));

        Assert.NotNull(translated);
        Assert.Empty(English.Keys.Except(translated.Keys));
        Assert.Empty(translated.Keys.Except(English.Keys));
        Assert.All(translated, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value), entry.Key));
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_KeepsThePlaceholdersAndTheAppName(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        var translated = Read(culture)!;

        Assert.All(English, entry =>
        {
            var text = translated[entry.Key];
            Assert.Equal(Placeholders(entry.Value), Placeholders(text));

            // A format string that string.Format can fill in (no stray braces).
            _ = string.Format(culture, text, "a", "b", "c");
            if (entry.Value.Contains("Shapeshifter", StringComparison.Ordinal))
            {
                Assert.Contains("Shapeshifter", text, StringComparison.Ordinal);
            }
        });
    }

    [Fact]
    public void EveryTranslationIsListed()
    {
        var unlisted = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(culture => !string.IsNullOrEmpty(culture.Name) && culture.Name != "en")
            .Where(HasSatelliteAssembly)
            .Select(culture => culture.Name)
            .Except(Localizer.Translations);

        Assert.Empty(unlisted);
    }

    [Fact]
    public void RelativeTime_SpeaksTheGivenLanguage()
    {
        var now = DateTimeOffset.Now;
        var german = RelativeTime.Describe(now.AddMinutes(-5), now, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(string.Format(CultureInfo.InvariantCulture, Localizer.Get(nameof(Strings.TimeMinutesAgo), CultureInfo.GetCultureInfo("de")), 5), german);
        Assert.NotEqual("5 min ago", german);
    }

    /// <summary>
    /// Whether a translation was built for exactly <paramref name="culture"/>. The resource manager is no good for this:
    /// once asked for "de-DE" it caches the German strings under that name too.
    /// </summary>
    private static bool HasSatelliteAssembly(CultureInfo culture)
    {
        try
        {
            _ = typeof(Strings).Assembly.GetSatelliteAssembly(culture);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    [Theory]
    [InlineData("ar", true)]
    [InlineData("fa", true)]
    [InlineData("de", false)]
    [InlineData("ja", false)]
    public void RightToLeft_FollowsTheLanguage(string name, bool expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            Assert.Equal(expected, Localizer.IsRightToLeft);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void ModifierKey_UsesTheLocalSpellingOfCtrl()
    {
        Assert.Equal(Strings.KeyControl, Localizer.ModifierKey("Ctrl"));
        Assert.Equal("Cmd", Localizer.ModifierKey("Cmd"));
    }
}
