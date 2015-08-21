using Autofac;
using Octokit;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string RepositoryOwner = "ffMathy";
        private const string RepositoryName = "Shapeshifter";

        private readonly GitHubClient client;

        private readonly IDownloader fileDownloader;
        private readonly IFileManager fileManager;

        public UpdateService(
            IDownloader fileDownloader,
            IFileManager fileManager)
        {
            client = CreateClient();

            this.fileDownloader = fileDownloader;
            this.fileManager = fileManager;
        }

        private async Task StartUpdateLoop()
        {
            while (true)
            {
                await UpdateAsync();
                await WaitForNextCycle();
            }
        }

        private static async Task WaitForNextCycle()
        {
            //TODO: introduce a circuit breaker for this.
            const int updateIntervalInHours = 3;

            const int milliSecondsInASecond = 1000;
            const int secondsInAMinute = 60;
            const int minutesInAnHour = 60;
            const int updateIntervalInMilliseconds = milliSecondsInASecond * secondsInAMinute * minutesInAnHour * updateIntervalInHours;

            await Task.Delay(updateIntervalInMilliseconds);
        }

        private Version GetCurrentVersion()
        {
            return GetAssemblyInformation().Version;
        }

        private string GetAssemblyName()
        {
            return GetAssemblyInformation().Name;
        }

        private static AssemblyName GetAssemblyInformation()
        {
            return Assembly.GetExecutingAssembly().GetName();
        }

        private GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(GetAssemblyName()));
            return client;
        }

        public async Task UpdateAsync()
        {
            var pendingUpdateRelease = await GetAvailableUpdateAsync();
            await UpdateFromReleaseAsync(pendingUpdateRelease);
        }

        private async Task UpdateFromReleaseAsync(Release pendingUpdateRelease)
        {
            var assets = await client.Release.GetAllAssets(RepositoryOwner, RepositoryName, pendingUpdateRelease.Id);
            await UpdateFromAssetsAsync(assets);
        }

        private async Task UpdateFromAssetsAsync(IReadOnlyList<ReleaseAsset> assets)
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

        private async Task UpdateFromAssetAsync(ReleaseAsset asset)
        {
            var localFilePath = await DownloadUpdateAsync(asset);
            var temporaryDirectory = ExtractUpdate(localFilePath);

            StartUpdate(temporaryDirectory);
        }

        private void StartUpdate(string temporaryDirectory)
        {
            var concretePath = Path.Combine(temporaryDirectory, GetAssemblyName() + ".exe");
            Process.Start(concretePath, $"update \"{Environment.CurrentDirectory}\"");
        }

        private async Task<string> DownloadUpdateAsync(ReleaseAsset asset)
        {
            var url = asset.BrowserDownloadUrl;

            var data = await fileDownloader.DownloadBytesAsync(url);
            var localFilePath = fileManager.WriteBytesToTemporaryFile(asset.Name, data);

            return localFilePath;
        }

        private string ExtractUpdate(string localFilePath)
        {
            var temporaryDirectory = fileManager.PrepareTemporaryPath("Update");
            ZipFile.ExtractToDirectory(localFilePath, temporaryDirectory);
            return temporaryDirectory;
        }

        private bool IsUpdateToReleaseNeeded(Release release)
        {
            if (release.Prerelease)
            {
                return false;
            }

            var releaseVersion = GetReleaseVersion(release);
            return IsUpdateToVersionNeeded(releaseVersion);
        }

        private bool IsUpdateToVersionNeeded(Version releaseVersion)
        {
            return releaseVersion > GetCurrentVersion();
        }

        private static Version GetReleaseVersion(Release release)
        {
            var versionMatch = Regex.Match(release.Name, "shapeshifter-v(.+)");
            var versionGroup = versionMatch.Groups[1];

            var version = new Version(versionGroup.Value);
            return version;
        }

        private async Task<Release> GetAvailableUpdateAsync()
        {
            var updates = await GetReleasesWithUpdatesÄsync();
            return updates
                .OrderByDescending(GetReleaseVersion)
                .FirstOrDefault();
        }

        private async Task<IEnumerable<Release>> GetReleasesWithUpdatesÄsync()
        {
            var allReleases = await client.Release.GetAll(RepositoryOwner, RepositoryName);
            return allReleases.Where(IsUpdateToReleaseNeeded);
        }

        public async void Start()
        {
            if (!Debugger.IsAttached)
            {
                await StartUpdateLoop();
            }
        }
    }
}
