namespace Shapeshifter.WindowsDesktop.Services.Updates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    using Octokit;

    [TestClass]
    public class UpdateServiceTest: TestBase
    {
        [TestMethod]
        public async Task UpdateNotFoundIfNoLaterVersionFound()
        {
            var fakeReleasesClient = Substitute.For<IReleasesClient>();
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Release
                .Returns(fakeReleasesClient);

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);
                });

            fakeReleasesClient
                .GetAll("ffMathy", "Shapeshifter")
                .Returns(
                    Task.FromResult<IReadOnlyList<Release>>(
                        new[]
                        {
                            CreateRelease(
                                1337, 
                                "shapeshifter-v0.0.0.0", 
                                false, 
                                false)
                        }));

            var updateService = container.Resolve<IUpdateService>();
            await updateService.UpdateAsync();

            fakeReleasesClient
                .DidNotReceive()
                .GetAllAssets(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>())
                .IgnoreAwait();
        }

        [TestMethod]
        public async Task UpdateNotFoundIfPrereleaseVersionFound()
        {
            var fakeReleasesClient = Substitute.For<IReleasesClient>();
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Release
                .Returns(fakeReleasesClient);

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);
                });

            fakeReleasesClient
                .GetAll("ffMathy", "Shapeshifter")
                .Returns(
                    Task.FromResult<IReadOnlyList<Release>>(
                        new[]
                        {
                            CreateRelease(
                                1337,
                                "shapeshifter-v1337.0.0.0",
                                true,
                                false)
                        }));

            var updateService = container.Resolve<IUpdateService>();
            await updateService.UpdateAsync();

            fakeReleasesClient
                .DidNotReceive()
                .GetAllAssets(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>())
                .IgnoreAwait();
        }

        [TestMethod]
        public async Task UpdateNotFoundIfDraftVersionFound()
        {
            var fakeReleasesClient = Substitute.For<IReleasesClient>();
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Release
                .Returns(fakeReleasesClient);

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);
                });

            fakeReleasesClient
                .GetAll("ffMathy", "Shapeshifter")
                .Returns(
                    Task.FromResult<IReadOnlyList<Release>>(
                        new[]
                        {
                            CreateRelease(
                                1337,
                                "shapeshifter-v1337.0.0.0",
                                false,
                                true)
                        }));

            var updateService = container.Resolve<IUpdateService>();
            await updateService.UpdateAsync();

            fakeReleasesClient
                .DidNotReceive()
                .GetAllAssets(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>())
                .IgnoreAwait();
        }

        static Release CreateRelease(int id, string name, bool isPrerelease, bool isDraft)
        {
            return new Release(
                "url",
                "htmlUrl",
                "assetsUrl",
                "uploadUrl",
                id,
                "tagName",
                "targetCommitish",
                name,
                "body",
                isDraft,
                isPrerelease,
                DateTimeOffset.Now,
                DateTimeOffset.Now);
        }
    }
}