using Shapeshifter.Core.Settings;

namespace Shapeshifter.Core.Tests.Settings;

[CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
public sealed class EnvironmentVariableCollection;

public sealed class AppPathsTests
{
    [Fact]
    public void ForRoot_LaysOutEveryFolderBelowTheRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ss-root");

        var paths = AppPaths.ForRoot(root);

        Assert.Equal(Path.Combine(root, "config"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(root, "data"), paths.DataDirectory);
        Assert.Equal(Path.Combine(root, "logs"), paths.LogDirectory);
        Assert.Equal(Path.Combine(root, "temp"), paths.TempDirectory);
        Assert.Equal(Path.Combine(root, "config", "settings.json"), paths.SettingsFile);
        Assert.Equal(Path.Combine(root, "data", "Pinned"), paths.PinnedDirectory);
        Assert.Equal(Path.Combine(root, "data", "instance.lock"), paths.InstanceLockFile);
    }

    [Fact]
    public void DerivedPaths_FollowTheConstructorArguments()
    {
        var paths = new AppPaths("/c", "/d", "/l", "/t");

        Assert.Equal("/c", paths.ConfigDirectory);
        Assert.Equal("/d", paths.DataDirectory);
        Assert.Equal("/l", paths.LogDirectory);
        Assert.Equal("/t", paths.TempDirectory);
        Assert.Equal(Path.Combine("/c", "settings.json"), paths.SettingsFile);
        Assert.Equal(Path.Combine("/d", "Pinned"), paths.PinnedDirectory);
        Assert.Equal(Path.Combine("/d", "instance.lock"), paths.InstanceLockFile);
    }

    [Fact]
    public void EnsureCreated_CreatesAllFolders_AndIsIdempotent()
    {
        var root = Directory.CreateTempSubdirectory("shapeshifter-paths-").FullName;
        try
        {
            var paths = AppPaths.ForRoot(root);

            paths.EnsureCreated();
            paths.EnsureCreated();

            Assert.True(Directory.Exists(paths.ConfigDirectory));
            Assert.True(Directory.Exists(paths.DataDirectory));
            Assert.True(Directory.Exists(paths.LogDirectory));
            Assert.True(Directory.Exists(paths.TempDirectory));
            Assert.False(Directory.Exists(paths.PinnedDirectory));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void HomeOverrideVariable_Name() => Assert.Equal("SHAPESHIFTER_HOME", AppPaths.HomeOverrideVariable);
}

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class AppPathsEnvironmentTests : IDisposable
{
    private readonly Dictionary<string, string?> saved = new(StringComparer.Ordinal);

    public AppPathsEnvironmentTests()
    {
        foreach (var name in new[] { AppPaths.HomeOverrideVariable, "XDG_CONFIG_HOME", "XDG_DATA_HOME" })
        {
            saved[name] = Environment.GetEnvironmentVariable(name);
        }
    }

    public void Dispose()
    {
        foreach (var (name, value) in saved)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    [Fact]
    public void CreateDefault_WithOverride_PutsEverythingBelowIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "ss-override-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(AppPaths.HomeOverrideVariable, root);

        var paths = AppPaths.CreateDefault();

        Assert.Equal(Path.Combine(root, "config"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(root, "data"), paths.DataDirectory);
        Assert.Equal(Path.Combine(root, "logs"), paths.LogDirectory);
        Assert.Equal(Path.Combine(root, "temp"), paths.TempDirectory);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void CreateDefault_WithRelativeOverride_ResolvesToAnAbsolutePath()
    {
        Environment.SetEnvironmentVariable(AppPaths.HomeOverrideVariable, "relative-home");

        var paths = AppPaths.CreateDefault();

        var expectedRoot = Path.GetFullPath("relative-home");
        Assert.True(Path.IsPathFullyQualified(paths.ConfigDirectory));
        Assert.Equal(Path.Combine(expectedRoot, "config"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(expectedRoot, "temp"), paths.TempDirectory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDefault_BlankOverride_IsIgnored(string value)
    {
        Environment.SetEnvironmentVariable(AppPaths.HomeOverrideVariable, value);

        var paths = AppPaths.CreateDefault();

        Assert.True(Path.IsPathFullyQualified(paths.ConfigDirectory));
        Assert.True(Path.IsPathFullyQualified(paths.TempDirectory));
        if (OperatingSystem.IsLinux())
        {
            // /tmp is shared between users on Linux; scratch files must stay in a per-user location.
            Assert.False(paths.TempDirectory.StartsWith("/tmp/", StringComparison.Ordinal));
        }
        else
        {
            // Windows and macOS temp folders are already per user.
            Assert.StartsWith(Path.GetTempPath(), paths.TempDirectory, StringComparison.Ordinal);
        }
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public void EnsureCreated_OnUnix_MakesFoldersPrivate()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix permissions only.");
        }

        var root = Path.Combine(Path.GetTempPath(), "shapeshifter-paths-" + Guid.NewGuid().ToString("N"));
        try
        {
            var paths = AppPaths.ForRoot(root);
            paths.EnsureCreated();
            foreach (var directory in new[] { paths.ConfigDirectory, paths.DataDirectory, paths.LogDirectory, paths.TempDirectory })
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(directory));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CreateDefault_OnLinux_HonoursXdgDirectories()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "XDG directories only apply on Linux.");
        Environment.SetEnvironmentVariable(AppPaths.HomeOverrideVariable, null);
        var config = Path.Combine(Path.GetTempPath(), "xdg-config");
        var data = Path.Combine(Path.GetTempPath(), "xdg-data");
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", config);
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", data);

        var paths = AppPaths.CreateDefault();

        Assert.Equal(Path.Combine(config, "shapeshifter"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(data, "shapeshifter"), paths.DataDirectory);
        Assert.Equal(Path.Combine(data, "shapeshifter", "logs"), paths.LogDirectory);
    }

    [Fact]
    public void CreateDefault_OnLinux_FallsBackToHomeWithoutXdg()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "XDG directories only apply on Linux.");
        Environment.SetEnvironmentVariable(AppPaths.HomeOverrideVariable, null);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", " ");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var paths = AppPaths.CreateDefault();

        Assert.Equal(Path.Combine(home, ".config", "shapeshifter"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(home, ".local", "share", "shapeshifter"), paths.DataDirectory);
    }
}
