using Autofac;
using Octokit;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class UpdateService : IUpdateService, IStartable
    {
        const string RepositoryOwner = "ffMathy";
        const string RepositoryName = "Shapeshifter";

        readonly GitHubClient client;

        readonly IDownloader fileDownloader;
        readonly IFileManager fileManager;
        readonly IEnvironmentInformation environmentInformation;
        readonly IProcessManager processManager;
        readonly ILogger logger;

        public UpdateService(
            IDownloader fileDownloader,
            IFileManager fileManager,
            IProcessManager processManager,
            IEnvironmentInformation environmentInformation,
            ILogger logger)
        {
            client = CreateClient();

            this.fileDownloader = fileDownloader;
            this.fileManager = fileManager;
            this.processManager = processManager;
            this.environmentInformation = environmentInformation;
            this.logger = logger;
        }

        async Task StartUpdateLoop()
        {
            while (true)
            {
                await UpdateAsync();
                await WaitForNextCycle();
            }
        }

        static async Task WaitForNextCycle()
        {
            //TODO: introduce a circuit breaker for this.
            const int updateIntervalInHours = 6;

            const int milliSecondsInASecond = 1000;
            const int secondsInAMinute = 60;
            const int minutesInAnHour = 60;
            const int updateIntervalInMilliseconds = milliSecondsInASecond * secondsInAMinute * minutesInAnHour * updateIntervalInHours;

            await Task.Delay(updateIntervalInMilliseconds);
        }

        Version GetCurrentVersion()
        {
            return GetAssemblyInformation().Version;
        }

        string GetAssemblyName()
        {
            return GetAssemblyInformation().Name;
        }

        static AssemblyName GetAssemblyInformation()
        {
            return Assembly.GetExecutingAssembly().GetName();
        }

        GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(GetAssemblyName()));
            return client;
        }

        public async Task UpdateAsync()
        {
            try {
                var pendingUpdateRelease = await GetAvailableUpdateAsync();
                await UpdateFromReleaseAsync(pendingUpdateRelease);
            } catch(RateLimitExceededException)
            {
                logger.Warning("Did not search for updates due to the GitHub rate limit being exceed.");
            }
        }

        async Task UpdateFromReleaseAsync(Release pendingUpdateRelease)
        {
            var assets = await client.Release.GetAllAssets(RepositoryOwner, RepositoryName, pendingUpdateRelease.Id);
            await UpdateFromAssetsAsync(assets);
        }

        async Task UpdateFromAssetsAsync(IReadOnlyList<ReleaseAsset> assets)
        {
            const string targetAssetName = "Binaries.zip";
            foreach (var asset in assets)
            {
                if (asset.Name == targetAssetName)
                {
                    await UpdateFromAssetAsync(asset);
                }
            }
        }

        async Task UpdateFromAssetAsync(ReleaseAsset asset)
        {
            var localFilePath = await DownloadUpdateAsync(asset);
            var temporaryDirectory = ExtractUpdate(localFilePath);

            StartUpdate(temporaryDirectory);
        }

        void StartUpdate(string temporaryDirectory)
        {
            var concretePath = Path.Combine(temporaryDirectory, GetAssemblyName() + ".exe");
            processManager.LaunchFile(concretePath, $"update \"{Environment.CurrentDirectory}\"");
        }

        async Task<string> DownloadUpdateAsync(ReleaseAsset asset)
        {
            var url = asset.BrowserDownloadUrl;

            var data = await fileDownloader.DownloadBytesAsync(url);
            var localFilePath = fileManager.WriteBytesToTemporaryFile(asset.Name, data);

            return localFilePath;
        }

        string ExtractUpdate(string localFilePath)
        {
            var temporaryDirectory = fileManager.PrepareTemporaryPath("Update");
            ZipFile.ExtractToDirectory(localFilePath, temporaryDirectory);
            return temporaryDirectory;
        }

        bool IsUpdateToReleaseNeeded(Release release)
        {
            if (release.Prerelease)
            {
                return false;
            }

            var releaseVersion = GetReleaseVersion(release);
            return IsUpdateToVersionNeeded(releaseVersion);
        }

        bool IsUpdateToVersionNeeded(Version releaseVersion)
        {
            return releaseVersion > GetCurrentVersion();
        }

        static Version GetReleaseVersion(Release release)
        {
            var versionMatch = Regex.Match(release.Name, "shapeshifter-v(.+)");
            var versionGroup = versionMatch.Groups[1];

            var version = new Version(versionGroup.Value);
            return version;
        }

        async Task<Release> GetAvailableUpdateAsync()
        {
            var updates = await GetReleasesWithUpdatesAsync();
            return updates
                .OrderByDescending(GetReleaseVersion)
                .FirstOrDefault();
        }

        async Task<IEnumerable<Release>> GetReleasesWithUpdatesAsync()
        {
            var allReleases = await client.Release.GetAll(RepositoryOwner, RepositoryName);
            return allReleases.Where(IsUpdateToReleaseNeeded);
        }

        public async void Start()
        {
            if (!environmentInformation.IsDebugging && !environmentInformation.IsInDesignTime)
            {
                await StartUpdateLoop();
            }
        }
    }
}
