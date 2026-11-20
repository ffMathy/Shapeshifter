using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Shapeshifter.Core.Updates;

namespace Shapeshifter.Core.Tests.Updates;

public sealed class ReleaseVersionTests
{
    [Theory]
    [InlineData("7.1.0", "7.1.0", "")]
    [InlineData("v7.1.3", "7.1.3", "")]
    [InlineData("7.2", "7.2.0", "")]
    [InlineData("7.2.0-beta.1", "7.2.0", "beta.1")]
    [InlineData("7.1.0+abc123", "7.1.0", "")]
    [InlineData("1.2.3.4", "1.2.3", "")]
    public void TryParse_ReadsTagsAndVersions(string text, string number, string prerelease)
    {
        var version = ReleaseVersion.TryParse(text);

        Assert.NotNull(version);
        Assert.Equal(Version.Parse(number), version.Number);
        Assert.Equal(prerelease, version.Prerelease);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("7")]
    [InlineData("7.x.0")]
    [InlineData("7.1.0-")]
    public void TryParse_RejectsWhatIsNoVersion(string? text) => Assert.Null(ReleaseVersion.TryParse(text));

    [Theory]
    [InlineData("7.1.0", "7.1.1")]
    [InlineData("7.1.9", "7.2.0")]
    [InlineData("7.2.0-beta.1", "7.2.0")]
    [InlineData("7.2.0-beta.1", "7.2.0-beta.2")]
    [InlineData("7.2.0-beta.2", "7.2.0-beta.10")]
    [InlineData("7.2.0-alpha", "7.2.0-beta")]
    [InlineData("7.2.0-1", "7.2.0-alpha")]
    [InlineData("0.1.9", "0.1.10")]
    public void Ordering_FollowsSemanticVersioning(string older, string newer)
    {
        var a = ReleaseVersion.TryParse(older)!;
        var b = ReleaseVersion.TryParse(newer)!;

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_IgnoresTheTagPrefixAndBuildMetadata() =>
        Assert.Equal(ReleaseVersion.TryParse("v7.1.0"), ReleaseVersion.TryParse("7.1.0+deadbeef"));

    [Theory]
    [InlineData("7.1.17-pr.635.98", true)]
    [InlineData("7.1.17-pr.635.98+abc123", true)]
    [InlineData("7.1.17", false)]
    [InlineData("7.2.0-beta.1", false)]
    [InlineData("7.2.0-preview.1", false)]
    public void IsPullRequestBuild_RecognisesTheVersionsCiGivesPullRequests(string text, bool expected) =>
        Assert.Equal(expected, ReleaseVersion.TryParse(text)!.IsPullRequestBuild);

    [Theory]
    [InlineData("7.1.16", "7.1.17-pr.635.98")]
    [InlineData("7.1.17-pr.635.98", "7.1.17-pr.635.99")]
    [InlineData("7.1.17-pr.635.99", "7.1.17")]
    public void PullRequestBuilds_AreNewerThanTheLastRelease_AndOlderThanTheNext(string older, string newer) =>
        Assert.True(ReleaseVersion.TryParse(older) < ReleaseVersion.TryParse(newer));
}

public sealed class RunningInstanceTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("instance-test").FullName;

    private string InfoFile => Path.Combine(root, "data", "instance.json");

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void WriteThenRead_RoundTrips()
    {
        var instance = new RunningInstance(1234, ReleaseVersion.TryParse("7.1.17-pr.635.98")!, "/opt/Shapeshifter");

        instance.Write(InfoFile);

        Assert.Equal(instance, RunningInstance.Read(InfoFile));
        Assert.Equal([InfoFile], Directory.GetFiles(Path.GetDirectoryName(InfoFile)!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"pid\":1,\"version\":\"latest\",\"path\":\"/x\"}")]
    [InlineData("{\"pid\":0,\"version\":\"7.1.0\",\"path\":\"/x\"}")]
    [InlineData("{\"pid\":1,\"version\":\"7.1.0\"}")]
    public void Read_ReturnsNull_ForAMissingOrBrokenRecord(string? content)
    {
        if (content is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InfoFile)!);
            File.WriteAllText(InfoFile, content);
        }

        Assert.Null(RunningInstance.Read(InfoFile));
    }

    [Fact]
    public void Remove_OnlyRemovesTheRecordOfThatProcess()
    {
        new RunningInstance(1, ReleaseVersion.TryParse("7.1.0")!, "/x").Write(InfoFile);

        RunningInstance.Remove(InfoFile, 2);
        Assert.True(File.Exists(InfoFile));

        RunningInstance.Remove(InfoFile, 1);
        Assert.False(File.Exists(InfoFile));
    }

    [Theory]
    [InlineData("7.1.17", "7.1.16", true)]
    [InlineData("7.1.17-pr.635.98", "7.1.16", true)]
    [InlineData("7.1.16", "7.1.16", false)]
    [InlineData("7.1.15", "7.1.16", false)]
    [InlineData("7.1.16", "7.1.17-pr.635.98", false)]
    public void ShouldReplace_OnlyWhenTheLaunchedCopyIsNewer(string launching, string running, bool expected) =>
        Assert.Equal(
            expected,
            RunningInstance.ShouldReplace(ReleaseVersion.TryParse(launching)!, new RunningInstance(1, ReleaseVersion.TryParse(running)!, "/x")));

    [Fact]
    public void ShouldReplace_NotWhenNothingIsKnownAboutTheRunningCopy() =>
        Assert.False(RunningInstance.ShouldReplace(ReleaseVersion.TryParse("99.0.0")!, null));
}

public sealed class InstallLayoutTests
{
    private static readonly AppIdentity App = TestApp.Identity;

    [Fact]
    public void Windows_InstallsBelowLocalAppDataPrograms()
    {
        var layout = InstallLayout.Create(App, InstallPlatform.Windows, "/home", "/local", "/state");

        Assert.Equal(Path.Combine("/local", "Programs", "Demo", "Demo.exe"), layout.Executable);
        Assert.Equal(layout.Executable, layout.Target);
        Assert.False(layout.TargetIsBundle);
        Assert.Equal(Path.Combine("/state", "install.json"), layout.StateFile);
    }

    [Fact]
    public void MacOS_InstallsAnAppBundleInTheUsersApplicationsFolder()
    {
        var layout = InstallLayout.Create(App, InstallPlatform.MacOS, "/Users/me", "/unused", "/state");

        Assert.Equal(Path.Combine("/Users/me", "Applications", "Demo.app"), layout.Target);
        Assert.Equal(Path.Combine("/Users/me", "Applications", "Demo.app", "Contents", "MacOS", "Demo"), layout.Executable);
        Assert.True(layout.TargetIsBundle);
    }

    [Fact]
    public void Linux_InstallsToLocalBin()
    {
        var layout = InstallLayout.Create(App, InstallPlatform.Linux, "/home/me", "/unused", "/state");

        Assert.Equal(Path.Combine("/home/me", ".local", "bin", "demo"), layout.Executable);
    }

    [Fact]
    public void IsInstalled_ComparesWithTheInstalledExecutable()
    {
        var layout = InstallLayout.Create(App, InstallPlatform.Linux, "/home/me", "/unused", "/state");

        Assert.True(layout.IsInstalled(new RunningApp("/home/me/.local/bin/demo", null, true)));
        Assert.False(layout.IsInstalled(new RunningApp("/home/me/Downloads/demo", null, true)));
        Assert.False(layout.IsInstalled(new RunningApp("/home/me/.local/bin/Demo", null, true)));
    }

    [Theory]
    [InlineData("/Applications/Demo.app/Contents/MacOS/Demo", "/Applications/Demo.app")]
    [InlineData("/Users/me/Downloads/Demo", null)]
    [InlineData("/Applications/Demo.app/Contents/MacOS/tools/helper", null)]
    public void BundleOf_FindsTheAppBundleAroundAnExecutable(string executable, string? bundle) =>
        Assert.Equal(bundle, RunningApp.BundleOf(executable));

    [Theory]
    [InlineData(InstallPlatform.Windows, Architecture.X64, "win-x64")]
    [InlineData(InstallPlatform.Windows, Architecture.Arm64, "win-arm64")]
    [InlineData(InstallPlatform.MacOS, Architecture.Arm64, "osx-arm64")]
    [InlineData(InstallPlatform.Linux, Architecture.X64, "linux-x64")]
    public void RuntimeIdentifier_MatchesTheReleaseBuilds(InstallPlatform platform, Architecture architecture, string expected) =>
        Assert.Equal(expected, RunningApp.RuntimeIdentifier(platform, architecture));
}

public sealed class SelfInstallerArgumentTests
{
    [Fact]
    public void ParseArguments_TakesOutTheInstallersOptions()
    {
        var (remaining, replaces, noInstall, uninstall, firstInstall) = SelfInstaller.ParseArguments(
            ["--show", "--replaces", "1234", "--no-install", "--first-install", "x"]);

        Assert.Equal(["--show", "x"], remaining);
        Assert.Equal(1234, replaces);
        Assert.True(noInstall);
        Assert.False(uninstall);
        Assert.True(firstInstall);
    }

    [Fact]
    public void ParseArguments_LeavesOtherArgumentsAlone()
    {
        var (remaining, replaces, _, uninstall, _) = SelfInstaller.ParseArguments(["--uninstall", "--replaces", "nope"]);

        Assert.Empty(remaining);
        Assert.Null(replaces);
        Assert.True(uninstall);
    }
}

public sealed class SelfInstallerTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("installer-test").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Install_CopiesTheRunningExecutableAndRecordsTheVersion()
    {
        if (OperatingSystem.IsMacOS())
        {
            return; // Installs a bundle with ditto; covered by the Linux and Windows layouts.
        }

        var platform = InstallLayout.CurrentPlatform;
        var layout = InstallLayout.Create(TestApp.Identity, platform, Path.Combine(root, "home"), Path.Combine(root, "local"), Path.Combine(root, "state"));
        var downloaded = Path.Combine(root, "Downloads", "demo");
        Directory.CreateDirectory(Path.GetDirectoryName(downloaded)!);
        File.WriteAllText(downloaded, "new build");
        var installer = new SelfInstaller(
            TestApp.Identity, layout, new RunningApp(downloaded, null, true), new RecordingHost(), new NullLog(), ReleaseVersion.TryParse("2.0.0"));

        installer.Install();

        Assert.Equal("new build", File.ReadAllText(layout.Executable));
        Assert.Equal(ReleaseVersion.TryParse("2.0.0"), SelfInstaller.ReadInstalledVersion(layout));
        Assert.Single(Directory.GetFileSystemEntries(Path.GetDirectoryName(layout.Executable)!));
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(File.GetUnixFileMode(layout.Executable).HasFlag(UnixFileMode.UserExecute));
        }
    }

    [Fact]
    public void Uninstall_RemovesTheInstallationAndItsState()
    {
        if (OperatingSystem.IsMacOS())
        {
            return;
        }

        var layout = InstallLayout.Create(TestApp.Identity, InstallLayout.CurrentPlatform, Path.Combine(root, "home"), Path.Combine(root, "local"), Path.Combine(root, "state"));
        Directory.CreateDirectory(Path.GetDirectoryName(layout.Executable)!);
        File.WriteAllText(layout.Executable, "installed");
        SelfInstaller.WriteInstalledVersion(layout, ReleaseVersion.TryParse("1.0.0")!);
        var host = new RecordingHost();
        var installer = new SelfInstaller(
            TestApp.Identity, layout, new RunningApp(Path.Combine(root, "elsewhere"), null, true), host, new NullLog(), ReleaseVersion.TryParse("1.0.0"));

        installer.Uninstall();

        Assert.False(File.Exists(layout.Executable));
        Assert.False(File.Exists(layout.StateFile));
        Assert.True(host.Stopped);
        Assert.True(host.AutostartRemoved);
    }

    [Fact]
    public void OnStartup_LeavesBuildsThatAreNotSingleFileAlone()
    {
        var layout = InstallLayout.Create(TestApp.Identity, InstallLayout.CurrentPlatform, Path.Combine(root, "home"), Path.Combine(root, "local"), Path.Combine(root, "state"));
        var installer = new SelfInstaller(
            TestApp.Identity, layout, new RunningApp(Path.Combine(root, "dev", "demo"), null, false), new RecordingHost(), new NullLog());

        var result = installer.OnStartup(["--show"]);

        Assert.False(result.Exit);
        Assert.False(result.IsInstalledCopy);
        Assert.Equal(["--show"], result.Arguments);
        Assert.False(File.Exists(layout.Executable));
    }

    [Fact]
    public void OnStartup_LeavesStoreInstallsAlone()
    {
        // The Microsoft Store and the Mac App Store install and update their copy; it must not copy itself elsewhere.
        var layout = InstallLayout.Create(TestApp.Identity, InstallLayout.CurrentPlatform, Path.Combine(root, "home"), Path.Combine(root, "local"), Path.Combine(root, "state"));
        var packaged = Path.Combine(root, "WindowsApps", "demo");
        Directory.CreateDirectory(Path.GetDirectoryName(packaged)!);
        File.WriteAllText(packaged, "store build");
        var host = new RecordingHost();
        var installer = new SelfInstaller(
            TestApp.Identity, layout, new RunningApp(packaged, null, IsSingleFile: true, IsStoreInstall: true), host, new NullLog(), ReleaseVersion.TryParse("2.0.0"));

        var result = installer.OnStartup(["--show"]);

        Assert.False(result.Exit);
        Assert.False(result.IsInstalledCopy);
        Assert.Equal(["--show"], result.Arguments);
        Assert.False(File.Exists(layout.Executable));
        Assert.Null(SelfInstaller.ReadInstalledVersion(layout));
        Assert.False(host.Stopped);
    }

    [Fact]
    public void OnStartup_RunsAPullRequestBuildWhereItIs_WithoutInstallingIt()
    {
        var layout = InstallLayout.Create(TestApp.Identity, InstallLayout.CurrentPlatform, Path.Combine(root, "home"), Path.Combine(root, "local"), Path.Combine(root, "state"));
        var downloaded = Path.Combine(root, "Downloads", "demo");
        Directory.CreateDirectory(Path.GetDirectoryName(downloaded)!);
        File.WriteAllText(downloaded, "pull request build");
        var host = new RecordingHost();
        var installer = new SelfInstaller(
            TestApp.Identity, layout, new RunningApp(downloaded, null, true), host, new NullLog(), ReleaseVersion.TryParse("7.1.17-pr.635.98"));

        var result = installer.OnStartup(["--show"]);

        Assert.False(result.Exit);
        Assert.False(result.IsInstalledCopy);
        Assert.Equal(["--show"], result.Arguments);
        Assert.False(File.Exists(layout.Executable));
        Assert.Null(SelfInstaller.ReadInstalledVersion(layout));
        Assert.False(host.Stopped);
    }

    [Fact]
    public void ReadInstalledVersion_IgnoresStateOfAnotherLocation()
    {
        var layout = InstallLayout.Create(TestApp.Identity, InstallPlatform.Linux, Path.Combine(root, "home"), root, Path.Combine(root, "state"));
        var other = InstallLayout.Create(TestApp.Identity, InstallPlatform.Linux, Path.Combine(root, "other"), root, Path.Combine(root, "state"));
        SelfInstaller.WriteInstalledVersion(other, ReleaseVersion.TryParse("9.0.0")!);

        Assert.Null(SelfInstaller.ReadInstalledVersion(layout));
        Assert.Equal(ReleaseVersion.TryParse("9.0.0"), SelfInstaller.ReadInstalledVersion(other));
    }
}

public sealed class FileSwapTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("swap-test").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Replace_PutsTheNewFileInPlaceAndLeavesNothingBehind()
    {
        var target = Path.Combine(root, "app");
        File.WriteAllText(target, "old");
        var staged = FileSwap.StagingPath(target);
        File.WriteAllText(staged, "new");

        FileSwap.Replace(staged, target);

        Assert.Equal("new", File.ReadAllText(target));
        Assert.Equal([target], Directory.GetFiles(root));
    }

    [Fact]
    public void Replace_WorksWhileTheOldFileIsOpen()
    {
        var target = Path.Combine(root, "app");
        File.WriteAllText(target, "old");
        using var running = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var staged = FileSwap.StagingPath(target);
        File.WriteAllText(staged, "new");

        FileSwap.Replace(staged, target);

        Assert.Equal("new", File.ReadAllText(target));
        using var reader = new StreamReader(running);
        Assert.Equal("old", reader.ReadToEnd());
    }

    [Fact]
    public void Replace_SwapsFolders()
    {
        var target = Path.Combine(root, "Demo.app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "version"), "old");
        var staged = FileSwap.StagingPath(target);
        Directory.CreateDirectory(staged);
        File.WriteAllText(Path.Combine(staged, "version"), "new");

        FileSwap.Replace(staged, target);

        Assert.Equal("new", File.ReadAllText(Path.Combine(target, "version")));
        Assert.Equal([target], Directory.GetDirectories(root));
    }

    [Fact]
    public void CleanLeftovers_RemovesOldAndUnfinishedCopiesOnly()
    {
        var target = Path.Combine(root, "app");
        File.WriteAllText(target, "current");
        File.WriteAllText(target + ".old-12345", "old");
        File.WriteAllText(target + ".new-abcde", "partial");
        File.WriteAllText(Path.Combine(root, "application"), "unrelated");

        FileSwap.CleanLeftovers(target);

        Assert.Equal([Path.Combine(root, "app"), Path.Combine(root, "application")], Directory.GetFiles(root).Order());
    }
}

public sealed class GitHubReleasesTests
{
    [Fact]
    public void Parse_ReadsTheReleaseAndItsAssets()
    {
        var release = GitHubReleases.Parse(TestApp.ReleaseJson("v2.1.0", ("Demo-linux-x64", 3, "sha256:" + new string('a', 64))));

        Assert.NotNull(release);
        Assert.Equal(ReleaseVersion.TryParse("2.1.0"), release.Version);
        Assert.Equal("v2.1.0", release.Tag);
        var asset = Assert.Single(release.Assets);
        Assert.Equal("Demo-linux-x64", asset.Name);
        Assert.Equal(3, asset.Size);
        Assert.Equal(new string('A', 64), asset.Sha256);
        Assert.Same(asset, release.Asset("demo-LINUX-x64"));
    }

    [Fact]
    public void Parse_IgnoresDraftsAndTagsThatAreNoVersion()
    {
        Assert.Null(GitHubReleases.Parse("""{"tag_name":"v1.0.0","draft":true,"html_url":"https://github.com/x/y"}"""));
        Assert.Null(GitHubReleases.Parse("""{"tag_name":"nightly","html_url":"https://github.com/x/y"}"""));
    }

    [Fact]
    public void FindChecksum_ReadsSha256SumOutput()
    {
        var a = new string('a', 64);
        var b = new string('b', 64);
        var sums = $"{a}  Demo-linux-x64\n{b} *Demo-win-x64.exe\n";

        Assert.Equal(a.ToUpperInvariant(), GitHubReleases.FindChecksum(sums, "Demo-linux-x64"));
        Assert.Equal(b.ToUpperInvariant(), GitHubReleases.FindChecksum(sums, "Demo-win-x64.exe"));
        Assert.Null(GitHubReleases.FindChecksum(sums, "Demo-linux"));
    }

    [Theory]
    [InlineData("md5:abc")]
    [InlineData("sha256:xyz")]
    [InlineData(null)]
    public void Sha256FromDigest_RejectsAnythingButSha256(string? digest) => Assert.Null(GitHubReleases.Sha256FromDigest(digest));
}

public sealed class UpdaterTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("updater-test").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public async Task CheckNow_ReplacesTheExecutableWithTheVerifiedDownload()
    {
        var executable = WriteExecutable("old build");
        var build = Encoding.UTF8.GetBytes("new build");
        using var http = new HttpClient(new FakeGitHub(TestApp.ReleaseJson("v2.0.0", ("Demo-linux-x64", build.Length, Digest(build))), build));
        using var updater = CreateUpdater(executable, http);
        ReleaseVersion? ready = null;
        updater.UpdateReady += (_, version) => ready = version;

        var updated = await updater.CheckNowAsync(TestContext.Current.CancellationToken);

        Assert.True(updated);
        Assert.Equal(UpdateState.ReadyToRestart, updater.State);
        Assert.Equal(ReleaseVersion.TryParse("2.0.0"), ready);
        Assert.Equal("new build", File.ReadAllText(executable));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(executable)!));
    }

    [Fact]
    public async Task CheckNow_UsesTheChecksumFileWhenGitHubReportsNoDigest()
    {
        var executable = WriteExecutable("old build");
        var build = Encoding.UTF8.GetBytes("new build");
        var sums = Convert.ToHexStringLower(SHA256.HashData(build)) + "  Demo-linux-x64\n";
        var json = TestApp.ReleaseJson("v2.0.0", ("Demo-linux-x64", build.Length, null), ("SHA256SUMS.txt", sums.Length, null));
        using var http = new HttpClient(new FakeGitHub(json, build, sums));
        using var updater = CreateUpdater(executable, http);

        Assert.True(await updater.CheckNowAsync(TestContext.Current.CancellationToken));
        Assert.Equal("new build", File.ReadAllText(executable));
    }

    [Fact]
    public async Task CheckNow_RefusesADownloadThatDoesNotMatchItsChecksum()
    {
        var executable = WriteExecutable("old build");
        var build = Encoding.UTF8.GetBytes("tampered");
        using var http = new HttpClient(new FakeGitHub(TestApp.ReleaseJson("v2.0.0", ("Demo-linux-x64", build.Length, Digest("new build"u8.ToArray()))), build));
        using var updater = CreateUpdater(executable, http);

        Assert.False(await updater.CheckNowAsync(TestContext.Current.CancellationToken));
        Assert.Equal(UpdateState.Failed, updater.State);
        Assert.Equal("old build", File.ReadAllText(executable));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(executable)!));
    }

    [Fact]
    public async Task CheckNow_RefusesAReleaseWithoutAnyChecksum()
    {
        var executable = WriteExecutable("old build");
        var build = Encoding.UTF8.GetBytes("new build");
        using var http = new HttpClient(new FakeGitHub(TestApp.ReleaseJson("v2.0.0", ("Demo-linux-x64", build.Length, null)), build));
        using var updater = CreateUpdater(executable, http);

        Assert.False(await updater.CheckNowAsync(TestContext.Current.CancellationToken));
        Assert.Equal(UpdateState.Failed, updater.State);
        Assert.Equal("old build", File.ReadAllText(executable));
    }

    [Fact]
    public async Task CheckNow_KeepsTheCurrentVersionWhenNothingNewerIsReleased()
    {
        var executable = WriteExecutable("old build");
        using var http = new HttpClient(new FakeGitHub(TestApp.ReleaseJson("v1.0.0"), []));
        using var updater = CreateUpdater(executable, http);

        Assert.False(await updater.CheckNowAsync(TestContext.Current.CancellationToken));
        Assert.Equal(UpdateState.UpToDate, updater.State);
        Assert.Equal("old build", File.ReadAllText(executable));
    }

    [Fact]
    public void Builds_ThatAreNotSingleFile_DoNotUpdate()
    {
        using var http = new HttpClient(new FakeGitHub("{}", []));
        using var updater = new Updater(
            TestApp.Identity, new RunningApp(Path.Combine(root, "demo"), null, false), null, http, new NullLog(), () => true,
            ReleaseVersion.TryParse("1.0.0"), "linux-x64");

        Assert.False(updater.IsSupported);
        Assert.Equal(UpdateState.Unsupported, updater.State);
    }

    [Fact]
    public void PullRequestBuilds_DoNotUpdate()
    {
        var executable = WriteExecutable("pull request build");
        using var http = new HttpClient(new FakeGitHub("{}", []));
        using var updater = new Updater(
            TestApp.Identity, new RunningApp(executable, null, true), null, http, new NullLog(), () => true,
            ReleaseVersion.TryParse("7.1.17-pr.635.98"), "linux-x64");

        Assert.False(updater.IsSupported);
        Assert.Equal(UpdateState.Unsupported, updater.State);
    }

    private string WriteExecutable(string content)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
        var executable = Path.Combine(directory, "demo");
        File.WriteAllText(executable, content);
        return executable;
    }

    private static Updater CreateUpdater(string executable, HttpClient http) => new(
        TestApp.Identity, new RunningApp(executable, null, true), null, http, new NullLog(), () => true,
        ReleaseVersion.TryParse("1.0.0"), "linux-x64");

    private static string Digest(byte[] content) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(content));

    /// <summary>Answers like api.github.com and its release downloads.</summary>
    private sealed class FakeGitHub(string releaseJson, byte[] build, string? checksums = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            HttpContent content = path switch
            {
                "/repos/owner/demo/releases/latest" => new StringContent(releaseJson),
                _ when path.EndsWith("/SHA256SUMS.txt", StringComparison.Ordinal) && checksums is not null => new StringContent(checksums),
                _ when path.EndsWith("/Demo-linux-x64", StringComparison.Ordinal) => new ByteArrayContent(build),
                _ => new StringContent(string.Empty),
            };
            var status = content is StringContent { Headers.ContentLength: 0 } ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }
    }
}

internal static class TestApp
{
    public static readonly AppIdentity Identity = new()
    {
        Name = "Demo",
        LinuxExecutableName = "demo",
        Description = "Demo app",
        GitHubOwner = "owner",
        GitHubRepository = "demo",
        ExecutableAssetName = rid => "Demo-" + rid + (rid.StartsWith("win-", StringComparison.Ordinal) ? ".exe" : string.Empty),
        DesktopEntryId = "demo",
        BundleIdentifier = "app.demo",
        EnvironmentPrefix = "DEMO_TEST",
    };

    public static string ReleaseJson(string tag, params (string Name, long Size, string? Digest)[] assets)
    {
        var list = string.Join(",", assets.Select(asset =>
            $$"""{"name":"{{asset.Name}}","size":{{asset.Size}},"browser_download_url":"https://github.com/owner/demo/releases/download/{{tag}}/{{asset.Name}}"{{(asset.Digest is null ? "" : $",\"digest\":\"{asset.Digest}\"")}}}"""));
        return $$"""{"tag_name":"{{tag}}","draft":false,"prerelease":false,"html_url":"https://github.com/owner/demo/releases/tag/{{tag}}","assets":[{{list}}]}""";
    }
}

internal sealed class NullLog : IUpdateLog
{
    public void Information(string message)
    {
    }

    public void Warning(string message, Exception? exception = null)
    {
    }
}

internal sealed class RecordingHost : IInstallHost
{
    public bool Stopped { get; private set; }

    public bool AutostartRemoved { get; private set; }

    public void StopRunningInstance() => Stopped = true;

    public void RemoveAutostart() => AutostartRemoved = true;
}
