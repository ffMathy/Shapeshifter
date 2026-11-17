namespace Shapeshifter.Core.Updates;

/// <summary>What the installer and the updater need to know about the application they install and update.</summary>
public sealed record AppIdentity
{
    /// <summary>Display name, also the name of the executable on Windows and macOS (<c>Shapeshifter.exe</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Name of the executable installed on Linux (<c>~/.local/bin/shapeshifter</c>).</summary>
    public required string LinuxExecutableName { get; init; }

    /// <summary>One-line description for the Start menu, the desktop entry and Apps &amp; features.</summary>
    public required string Description { get; init; }

    /// <summary>Owner of the GitHub repository whose releases are installed (<c>ffMathy</c>).</summary>
    public required string GitHubOwner { get; init; }

    /// <summary>Name of the GitHub repository whose releases are installed (<c>Shapeshifter</c>).</summary>
    public required string GitHubRepository { get; init; }

    /// <summary>Release asset of the executable for a runtime identifier such as <c>win-x64</c>.</summary>
    public required Func<string, string> ExecutableAssetName { get; init; }

    /// <summary>Release asset of the zipped macOS app bundle for a runtime identifier, when releases have one.</summary>
    public Func<string, string>? MacBundleAssetName { get; init; }

    /// <summary>Name of the freedesktop.org desktop entry, without <c>.desktop</c>; also the icon name.</summary>
    public required string DesktopEntryId { get; init; }

    /// <summary>Extra lines for the desktop entry (<c>Categories=…</c>, <c>StartupWMClass=…</c>).</summary>
    public IReadOnlyList<string> DesktopEntryLines { get; init; } = [];

    /// <summary>Arguments the Start menu shortcut and the desktop entry start the app with.</summary>
    public IReadOnlyList<string> LaunchArguments { get; init; } = [];

    /// <summary>CFBundleIdentifier of the app bundle created on macOS.</summary>
    public required string BundleIdentifier { get; init; }

    /// <summary>Prefix of the environment variables that turn installing and updating off (<c>SHAPESHIFTER</c>).</summary>
    public required string EnvironmentPrefix { get; init; }

    /// <summary>The application icon as a PNG, for the Linux desktop entry. Null when there is none.</summary>
    public Func<Stream?>? OpenIconPng { get; init; }

    /// <summary>Set to 1 to keep the app where it was started instead of installing it.</summary>
    public string NoInstallVariable => EnvironmentPrefix + "_NO_INSTALL";

    /// <summary>Set to 1 to never check GitHub for updates.</summary>
    public string NoUpdateVariable => EnvironmentPrefix + "_NO_UPDATE";

    /// <summary>The GitHub repository's page.</summary>
    public Uri ProjectPage => new($"https://github.com/{GitHubOwner}/{GitHubRepository}");

    /// <summary>The GitHub page that lists the releases.</summary>
    public Uri ReleasesPage => new($"https://github.com/{GitHubOwner}/{GitHubRepository}/releases");
}
