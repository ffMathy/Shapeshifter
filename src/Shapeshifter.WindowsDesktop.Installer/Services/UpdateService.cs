namespace Shapeshifter.UserInterface.WindowsDesktop.Installer.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Interfaces;

    using Shapeshifter.WindowsDesktop.Shared.Infrastructure.Logging.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Services.Files.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Services.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Services.Web.Interfaces;

    using Octokit;

    class UpdateService
        : IUpdateService
    {
        const string RepositoryOwner = "ffMathy";
        const string RepositoryName = "Shapeshifter";

        readonly GitHubClient client;

        readonly IDownloader fileDownloader;
        readonly IFileManager fileManager;
        readonly IProcessManager processManager;
        readonly IReleaseVersionManager releaseVersionManager;
        readonly ILogger logger;

        public UpdateService(
            IDownloader fileDownloader,
            IFileManager fileManager,
            IProcessManager processManager,
            IReleaseVersionManager releaseVersionManager,
            ILogger logger)
        {
            client = CreateClient();

            this.fileDownloader = fileDownloader;
            this.fileManager = fileManager;
            this.processManager = processManager;
            this.releaseVersionManager = releaseVersionManager;
            this.logger = logger;
        }

        static GitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(RepositoryName));
            return client;
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

        async Task UpdateFromAssetsAsync(IReadOnlyList<ReleaseAsset> assets)
        {
            const string targetAssetName = "Binaries.zip";

            var asset = assets.Single(x => x.Name == targetAssetName);
            await UpdateFromAssetAsync(asset);
        }

        async Task UpdateFromAssetAsync(ReleaseAsset asset)
        {
            var localFilePath = await DownloadUpdateAsync(asset);
            var temporaryDirectory = ExtractUpdate(localFilePath);

            StartUpdate(temporaryDirectory);
        }

        void StartUpdate(string temporaryDirectory)
        {
            var filesInDirectory = Directory.GetFiles(temporaryDirectory);
            var executablePath = filesInDirectory.Single(
                x => Path.GetExtension(x) == ".exe");
            processManager.LaunchFile(
                executablePath,
                $"update");
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
            var temporaryDirectory = fileManager.PrepareTemporaryFolder("Update");
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
            return releaseVersion > releaseVersionManager.GetCurrentVersion();
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
                RepositoryOwner, RepositoryName);
            return allReleases.Where(IsUpdateToReleaseNeeded);
        }
    }
}