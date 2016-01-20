using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services.Updates
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
