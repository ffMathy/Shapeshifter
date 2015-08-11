using Octokit;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class UpdateService : IUpdateService
    {
        private readonly GitHubClient client;

        public UpdateService()
        {
            this.client = CreateClient();

            Task.Run(StartUpdateLoop);
        }

        private async Task StartUpdateLoop()
        {
            while (true)
            {
                var releases = await client.Release.GetAll("ffMathy", "Shapeshifter");
                foreach (var release in releases)
                {
                    if (!release.Prerelease)
                    {
                        var versionMatch = Regex.Match(release.Name, "shapeshifter-v(.+)");
                        var versionGroup = versionMatch.Groups[1];
                        var version = new Version(versionGroup.Value);
                        if(version > GetCurrentVersion())
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
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

        public async Task<bool> HasUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync()
        {
            throw new NotImplementedException();
        }
    }
}
