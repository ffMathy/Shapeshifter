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
        const string RepositoryOwner = "ffMathy";
        const string RepositoryName = "Shapeshifter";

        readonly GitHubClient client;

        readonly IDownloader fileDownloader;
        readonly IFileManager fileManager;

        public UpdateService(
            IDownloader fileDownloader,
            IFileManager fileManager)
        {
            qwd´kqwdkwq

                //TODO: do not load this in designer time and other things. make fake services.

            client = CreateClient();

            this.fileDownloader = fileDownloader;
            this.fileManager = fileManager;
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
            const int updateIntervalInHours = 3;

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
            var pendingUpdateRelease = await GetAvailableUpdateAsync();
            await UpdateFromReleaseAsync(pendingUpdateRelease);
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
            Process.Start(concretePath, $"update \"{Environment.CurrentDirectory}\"");
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
            var updates = await GetReleasesWithUpdatesÄsync();
            return updates
                .OrderByDescending(GetReleaseVersion)
                .FirstOrDefault();
        }

        async Task<IEnumerable<Release>> GetReleasesWithUpdatesÄsync()
        {
            var allReleases = await client.Release.GetAll(RepositoryOwner, RepositoryName);
            return allReleases.Where(IsUpdateToReleaseNeeded);
        }

        public async void Start()
        {
            await StartUpdateLoop();
        }
    }
}
