namespace Shapeshifter.WindowsDesktop.Services.Updates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Files.Interfaces;

    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Octokit;

    using Services.Interfaces;

    using Web.Interfaces;

    class UpdateService
        : IUpdateService
    {
        const string RepositoryOwner = "ffMathy";
        const string RepositoryName = "Shapeshifter";

        readonly IGitHubClient client;

        readonly IDownloader fileDownloader;
        readonly IFileManager fileManager;
        readonly IProcessManager processManager;
        readonly ILogger logger;

        public UpdateService(
            IDownloader fileDownloader,
            IFileManager fileManager,
            IProcessManager processManager,
            ILogger logger,
            IGitHubClientFactory clientFactory)
        {
            client = clientFactory.CreateClient();

            this.fileDownloader = fileDownloader;
            this.fileManager = fileManager;
            this.processManager = processManager;
            this.logger = logger;
        }

        public async Task UpdateAsync()
        {
            try
            {
                var pendingUpdateRelease = await GetAvailableUpdateAsync();
                if (pendingUpdateRelease == null)
                {
                    return;
                }

                await UpdateFromReleaseAsync(pendingUpdateRelease);
            }
            catch (RateLimitExceededException)
            {
                logger.Warning(
                    "Did not search for updates due to the GitHub rate limit being exceed.");
            }
            catch (HttpRequestException)
            {
                logger.Warning(
                    "Did not search for updates since there was no Internet connection.");
            }
        }

        async Task UpdateFromReleaseAsync(Release pendingUpdateRelease)
        {
            var assets =
                await client.Release.GetAllAssets(
                    RepositoryOwner,
                    RepositoryName,
                    pendingUpdateRelease.Id);
            await UpdateFromAssetsAsync(assets);
        }

        async Task UpdateFromAssetsAsync(IEnumerable<ReleaseAsset> assets)
        {
            const string targetAssetName = "Shapeshifter.exe";

            var asset = assets.Single(x => x.Name == targetAssetName);
            await UpdateFromAssetAsync(asset);
        }

        async Task UpdateFromAssetAsync(ReleaseAsset asset)
        {
            var localFilePath = await DownloadUpdateToDiskAsync(asset);
            processManager.LaunchFile(
                localFilePath,
                "update");
        }

        async Task<string> DownloadUpdateToDiskAsync(ReleaseAsset asset)
        {
            var data = await fileDownloader.DownloadBytesAsync(
                asset.BrowserDownloadUrl);
            var localFilePath = fileManager.WriteBytesToTemporaryFile(
                asset.Name, data);

            return localFilePath;
        }

        bool IsUpdateToReleaseNeeded(Release release)
        {
            if (release.Prerelease)
            {
                return false;
            }

            if (release.Draft)
            {
                return false;
            }

            var releaseVersion = GetReleaseVersion(release);
            return IsUpdateToVersionNeeded(releaseVersion);
        }

        public Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly
                .GetName()
                .Version;
        }

        bool IsUpdateToVersionNeeded(Version releaseVersion)
        {
            return releaseVersion > GetCurrentVersion();
        }

        static Version GetReleaseVersion(Release release)
        {
            var versionMatch = Regex.Match(release.Name, @"shapeshifter-v([\.\d]+)");
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
            var allReleases = await client.Release.GetAll(
                RepositoryOwner,
                RepositoryName);
            return allReleases.Where(IsUpdateToReleaseNeeded);
        }
    }
}