using Avalonia.Data.Converters;

namespace Shapeshifter.Desktop;

/// <summary>Converters used from XAML.</summary>
public static class OpacityConverters
{
    /// <summary><see langword="true"/> becomes fully opaque, <see langword="false"/> fully transparent.</summary>
    public static IValueConverter FromBool { get; } =
        new FuncValueConverter<bool, double>(value => value ? 1.0 : 0.0);
}
