using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Core.Updates;

public enum UpdateState
{
    /// <summary>Not checked yet.</summary>
    Idle,

    /// <summary>This build cannot update itself (not a release build, or its folder is read-only).</summary>
    Unsupported,

    /// <summary>The user turned automatic updates off.</summary>
    Disabled,

    Checking,
    UpToDate,
    Downloading,

    /// <summary>A newer version replaced the executable; it runs after a restart.</summary>
    ReadyToRestart,
    Failed,
}

/// <summary>
/// Keeps the application up to date from its GitHub releases: checks the latest release now and then, downloads the
/// build for this computer, verifies its SHA-256, puts it in place of the running executable and reports that a
/// restart will run it. The application decides when to restart (<see cref="Restart"/>).
/// </summary>
public sealed class Updater : IDisposable
{
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromHours(1);

    private readonly AppIdentity app;
    private readonly RunningApp running;
    private readonly InstallLayout? layout;
    private readonly GitHubReleases releases;
    private readonly HttpClient http;
    private readonly IUpdateLog log;
    private readonly Func<bool> isEnabled;
    private readonly ReleaseVersion current;
    private readonly string runtimeIdentifier;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private readonly Lock stateLock = new();
    private string? unsupportedReason;

    /// <param name="layout">Where the app is installed, to record the new version there; null when not installed.</param>
    /// <param name="isEnabled">Whether the user wants automatic updates (read before every check).</param>
    public Updater(
        AppIdentity app,
        RunningApp running,
        InstallLayout? layout,
        HttpClient http,
        IUpdateLog log,
        Func<bool> isEnabled,
        ReleaseVersion? current = null,
        string? runtimeIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        this.app = app;
        this.running = running;
        this.layout = layout;
        this.http = http;
        this.log = log;
        this.isEnabled = isEnabled;
        this.current = current ?? ReleaseVersion.Current();
        this.runtimeIdentifier = runtimeIdentifier ?? RunningApp.RuntimeIdentifier(InstallLayout.CurrentPlatform, RuntimeInformation.OSArchitecture);
        releases = new GitHubReleases(http, app.GitHubOwner, app.GitHubRepository);
        unsupportedReason = FindUnsupportedReason();
        State = unsupportedReason is null ? UpdateState.Idle : UpdateState.Unsupported;
    }

    /// <summary>Raised on a background thread whenever <see cref="State"/> or <see cref="StatusText"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised on a background thread once a newer version is in place and a restart would run it.</summary>
    public event EventHandler<ReleaseVersion>? UpdateReady;

    public UpdateState State { get; private set; }

    /// <summary>The version that runs after a restart, once <see cref="State"/> is <see cref="UpdateState.ReadyToRestart"/>.</summary>
    public ReleaseVersion? PendingVersion { get; private set; }

    public DateTimeOffset? LastChecked { get; private set; }

    public string? LastError { get; private set; }

    public ReleaseVersion CurrentVersion => current;

    /// <summary>False for builds that cannot replace themselves (see <see cref="UpdateState.Unsupported"/>).</summary>
    public bool IsSupported => unsupportedReason is null;

    /// <summary>One line for the settings window.</summary>
    public string StatusText
    {
        get
        {
            lock (stateLock)
            {
                if (State is UpdateState.Idle or UpdateState.UpToDate or UpdateState.Disabled && !isEnabled())
                {
                    return Strings.UpdateOff;
                }

                return State switch
                {
                    UpdateState.Unsupported => unsupportedReason ?? Strings.UpdateUnsupported,
                    UpdateState.Disabled => Strings.UpdateOff,
                    UpdateState.Checking => Strings.UpdateChecking,
                    UpdateState.UpToDate => Localizer.Format(Strings.UpdateUpToDate, LastChecked?.ToLocalTime().ToString("t", CultureInfo.CurrentCulture)),
                    UpdateState.Downloading => Localizer.Format(Strings.UpdateDownloading, PendingVersion),
                    UpdateState.ReadyToRestart => Localizer.Format(Strings.UpdateReadyToRestart, PendingVersion, app.Name),
                    UpdateState.Failed => Localizer.Format(Strings.UpdateFailed, LastError),
                    _ => Strings.UpdateAutomatic,
                };
            }
        }
    }

    /// <summary>Checks after <paramref name="firstDelay"/> and then every <paramref name="interval"/>.</summary>
    public void Start(TimeSpan firstDelay, TimeSpan interval)
    {
        if (!IsSupported)
        {
            log.Information($"Automatic updates are not available: {unsupportedReason}");
            return;
        }

        var token = stopping.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(firstDelay, token).ConfigureAwait(false);
                while (!token.IsCancellationRequested)
                {
                    if (isEnabled())
                    {
                        await CheckNowAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        SetState(UpdateState.Disabled);
                    }

                    if (State == UpdateState.ReadyToRestart)
                    {
                        return;
                    }

                    await Task.Delay(interval, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    /// <summary>Checks for an update now and installs it. Returns true when a newer version is ready to run.</summary>
    public async Task<bool> CheckNowAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return false;
        }

        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return false; // A check is already running.
        }

        try
        {
            if (State == UpdateState.ReadyToRestart)
            {
                return true;
            }

            SetState(UpdateState.Checking);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token);
            timeout.CancelAfter(ApiTimeout);
            var release = await releases.GetLatestAsync(timeout.Token).ConfigureAwait(false);
            LastChecked = DateTimeOffset.Now;
            if (release is null || release.Version <= current)
            {
                SetState(UpdateState.UpToDate);
                return false;
            }

            log.Information($"{app.Name} {release.Version} is available (running {current})");
            PendingVersion = release.Version;
            SetState(UpdateState.Downloading);
            await InstallAsync(release, cancellationToken).ConfigureAwait(false);
            if (layout is not null && layout.IsInstalled(running))
            {
                SelfInstaller.WriteInstalledVersion(layout, release.Version);
            }

            log.Information($"{app.Name} {release.Version} is in place; it runs after a restart");
            SetState(UpdateState.ReadyToRestart);
            UpdateReady?.Invoke(this, release.Version);
            return true;
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            SetState(UpdateState.Idle);
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException
                                              or InvalidOperationException or OperationCanceledException or Win32Exception
                                              or System.Text.Json.JsonException or CryptographicException)
        {
            LastError = exception is OperationCanceledException ? "GitHub did not answer in time" : exception.Message;
            log.Warning("Could not update", exception);
            SetState(UpdateState.Failed);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Starts the new version, which waits for this process to exit. The caller then shuts the application down.
    /// </summary>
    public void Restart(IEnumerable<string> arguments)
    {
        using var _ = SelfInstaller.Launch(
            running.ExecutablePath,
            [SelfInstaller.ReplacesOption, Environment.ProcessId.ToString(CultureInfo.InvariantCulture), .. arguments]);
    }

    public void Dispose()
    {
        stopping.Cancel();
        stopping.Dispose();
    }

    /// <summary>The release file for this computer: the zipped app bundle when running from one, else the executable.</summary>
    internal string AssetName =>
        running.BundlePath is not null && app.MacBundleAssetName is { } bundleAsset
            ? bundleAsset(runtimeIdentifier)
            : app.ExecutableAssetName(runtimeIdentifier);

    private async Task InstallAsync(ReleaseInfo release, CancellationToken cancellationToken)
    {
        var asset = release.Asset(AssetName)
            ?? throw new InvalidOperationException($"Version {release.Version} has no build for {runtimeIdentifier} ({AssetName})");

        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token))
        {
            timeout.CancelAfter(ApiTimeout);
            var expected = await releases.GetSha256Async(release, asset, timeout.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Version {release.Version} publishes no checksum for {asset.Name}");
            await DownloadAndReplaceAsync(asset, expected, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DownloadAndReplaceAsync(ReleaseAsset asset, string expectedSha256, CancellationToken cancellationToken)
    {
        var target = AssetName == app.ExecutableAssetName(runtimeIdentifier) ? running.ExecutablePath : running.ReplaceablePath;
        var isBundle = !string.Equals(target, running.ExecutablePath, StringComparison.Ordinal);
        var download = FileSwap.StagingPath(target) + (isBundle ? ".zip" : string.Empty);
        string? extracted = null;
        try
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token))
            {
                timeout.CancelAfter(DownloadTimeout);
                await DownloadAsync(asset, expectedSha256, download, timeout.Token).ConfigureAwait(false);
            }

            string staged;
            if (isBundle)
            {
                extracted = FileSwap.StagingPath(target);
                FileSwap.RunTool("/usr/bin/ditto", "-x", "-k", download, extracted);
                staged = Directory.EnumerateDirectories(extracted, "*.app").SingleOrDefault()
                    ?? throw new InvalidOperationException($"{asset.Name} holds no app bundle");
                if (!File.Exists(Path.Combine(staged, "Contents", "MacOS", Path.GetFileName(running.ExecutablePath))))
                {
                    throw new InvalidOperationException($"{asset.Name} holds no {Path.GetFileName(running.ExecutablePath)}");
                }
            }
            else
            {
                FileSwap.MakeExecutable(download);
                staged = download;
            }

            FileSwap.Replace(staged, target);
        }
        finally
        {
            FileSwap.TryDelete(download);
            if (extracted is not null)
            {
                FileSwap.TryDelete(extracted);
            }
        }
    }

    private async Task DownloadAsync(ReleaseAsset asset, string expectedSha256, string destination, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long total = 0;
        var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true);
            await using (file.ConfigureAwait(false))
            {
                var buffer = new byte[1 << 16];
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    total += read;
                }
            }
        }

        if (asset.Size >= 0 && total != asset.Size)
        {
            throw new InvalidOperationException($"The download of {asset.Name} is incomplete ({total} of {asset.Size} bytes)");
        }

        var actual = Convert.ToHexString(hash.GetHashAndReset());
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new CryptographicException($"{asset.Name} does not match its published SHA-256 checksum");
        }
    }

    private string? FindUnsupportedReason()
    {
        if (Environment.GetEnvironmentVariable(app.NoUpdateVariable) == "1")
        {
            return Localizer.Format(Strings.UpdateTurnedOff, app.NoUpdateVariable);
        }

        if (running.IsStoreInstall)
        {
            return Strings.UpdateUnsupported;
        }

        if (!running.IsSingleFile)
        {
            return Strings.UpdateReleaseBuildsOnly;
        }

        if (current.IsPullRequestBuild)
        {
            return Strings.UpdatePullRequestBuild;
        }

        var directory = Path.GetDirectoryName(running.ReplaceablePath);
        if (directory is null || !CanWrite(directory))
        {
            return Localizer.Format(Strings.UpdateCannotWrite, app.Name, directory);
        }

        return null;
    }

    private static bool CanWrite(string directory)
    {
        var probe = Path.Combine(directory, ".write-test-" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        try
        {
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void SetState(UpdateState state)
    {
        lock (stateLock)
        {
            State = state;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
