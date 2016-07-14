namespace Shapeshifter.WindowsDesktop.Services.Web.Updates.Interfaces
{
    using Octokit;

    public interface IGitHubClientFactory
    {
        IGitHubClient CreateClient();
    }
}