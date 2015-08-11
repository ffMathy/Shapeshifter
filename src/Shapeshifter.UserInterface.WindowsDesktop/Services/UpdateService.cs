using Octokit;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class UpdateService : IUpdateService
    {
        private const string RepositoryOwner = "ffMathy";
        private const string RepositoryName = "Shapeshifter";

        private readonly GitHubClient client;

        private readonly IFileDownloader fileDownloader;

        public UpdateService(IFileDownloader fileDownloader)
        {
            client = CreateClient();

            this.fileDownloader = fileDownloader;

            Task.Run(StartUpdateLoop);
        }

        private async Task StartUpdateLoop()
        {
            while (true)
            {
                await UpdateAsync();

                const int threeHoursInMilliseconds = 1 * 1000 * 60 * 60 * 3;
                await Task.Delay(threeHoursInMilliseconds);
            }
        }

        private Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        private GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue("Shapeshifter"));
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
            UpdateFromAssets(assets);
        }

        private static void UpdateFromAssets(IReadOnlyList<ReleaseAsset> assets)
        {
            foreach (var asset in assets)
            {
                if (asset.Name == "Binaries.zip")
                {
                    var url = asset.BrowserDownloadUrl;
                    //TODO
                }
            }
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
    }
}
