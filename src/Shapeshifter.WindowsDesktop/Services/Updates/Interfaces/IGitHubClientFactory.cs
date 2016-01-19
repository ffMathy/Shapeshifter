namespace Shapeshifter.WindowsDesktop.Services.Updates.Interfaces
{
    using Octokit;

    public interface IGitHubClientFactory
    {
        IGitHubClient CreateClient();
    }
}
