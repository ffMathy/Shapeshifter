using System.Globalization;
using System.Reflection;

namespace Shapeshifter.Core.Updates;

/// <summary>
/// A release version such as <c>7.1.0</c> or <c>v7.2.0-beta.1</c>, ordered like semantic versions: a prerelease comes
/// before the release of the same number, and prerelease identifiers compare numerically where they are numbers.
/// </summary>
public sealed class ReleaseVersion : IComparable<ReleaseVersion>, IEquatable<ReleaseVersion>
{
    private ReleaseVersion(Version number, string prerelease)
    {
        Number = number;
        Prerelease = prerelease;
    }

    /// <summary>Major, minor and patch (missing parts are 0).</summary>
    public Version Number { get; }

    /// <summary>The part after '-', or empty for a release.</summary>
    public string Prerelease { get; }

    public bool IsPrerelease => Prerelease.Length > 0;

    /// <summary>Parses <c>1.2</c>, <c>1.2.3</c>, <c>v1.2.3</c> and <c>1.2.3-rc.1+build</c>; null for anything else.</summary>
    public static ReleaseVersion? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var value = text.Trim();
        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        var plus = value.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
        {
            value = value[..plus];
        }

        var prerelease = string.Empty;
        var dash = value.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            prerelease = value[(dash + 1)..];
            value = value[..dash];
            if (prerelease.Length == 0)
            {
                return null;
            }
        }

        var parts = value.Split('.');
        if (parts.Length is < 2 or > 4)
        {
            return null;
        }

        var numbers = new int[3];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var part))
            {
                return null;
            }

            if (i < 3)
            {
                numbers[i] = part;
            }
        }

        return new ReleaseVersion(new Version(numbers[0], numbers[1], numbers[2]), prerelease);
    }

    /// <summary>The version of the running application, from its informational version (what <c>-p:Version</c> sets).</summary>
    public static ReleaseVersion Current()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(ReleaseVersion).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return TryParse(informational)
            ?? TryParse(assembly.GetName().Version?.ToString(3))
            ?? new ReleaseVersion(new Version(0, 0, 0), string.Empty);
    }

    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var byNumber = Number.CompareTo(other.Number);
        if (byNumber != 0)
        {
            return byNumber;
        }

        if (!IsPrerelease || !other.IsPrerelease)
        {
            // A release is newer than its prereleases.
            return other.IsPrerelease.CompareTo(IsPrerelease);
        }

        var mine = Prerelease.Split('.');
        var theirs = other.Prerelease.Split('.');
        for (var i = 0; i < Math.Min(mine.Length, theirs.Length); i++)
        {
            var mineIsNumber = long.TryParse(mine[i], NumberStyles.None, CultureInfo.InvariantCulture, out var mineNumber);
            var theirsIsNumber = long.TryParse(theirs[i], NumberStyles.None, CultureInfo.InvariantCulture, out var theirsNumber);
            var result = (mineIsNumber, theirsIsNumber) switch
            {
                (true, true) => mineNumber.CompareTo(theirsNumber),
                (true, false) => -1,
                (false, true) => 1,
                _ => string.CompareOrdinal(mine[i], theirs[i]),
            };
            if (result != 0)
            {
                return result;
            }
        }

        return mine.Length.CompareTo(theirs.Length);
    }

    public bool Equals(ReleaseVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is ReleaseVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Number, Prerelease);

    public override string ToString() => IsPrerelease ? $"{Number.ToString(3)}-{Prerelease}" : Number.ToString(3);

    public static bool operator ==(ReleaseVersion? left, ReleaseVersion? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ReleaseVersion? left, ReleaseVersion? right) => !(left == right);

    public static bool operator <(ReleaseVersion? left, ReleaseVersion? right) => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator >(ReleaseVersion? left, ReleaseVersion? right) => left is not null && left.CompareTo(right) > 0;

    public static bool operator <=(ReleaseVersion? left, ReleaseVersion? right) => !(left > right);

    public static bool operator >=(ReleaseVersion? left, ReleaseVersion? right) => !(left < right);
}
