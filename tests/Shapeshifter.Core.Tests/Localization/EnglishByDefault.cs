using System.Globalization;
using System.Runtime.CompilerServices;

namespace Shapeshifter.Core.Tests.Localization;

internal static class EnglishByDefault
{
    /// <summary>
    /// The tests compare against the English text, so they read the neutral resources whatever the display language
    /// of the machine that runs them.
    /// </summary>
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Sets the culture for the whole test assembly.")]
    internal static void Initialize()
    {
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
