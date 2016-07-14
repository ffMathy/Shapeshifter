namespace Shapeshifter.WindowsDesktop.Services.Web.Updates
{
    using Interfaces;

    using Octokit;

    class GitHubClientFactory: IGitHubClientFactory
    {
        public IGitHubClient CreateClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(nameof(Shapeshifter)));
            return client;
        }
    }
}