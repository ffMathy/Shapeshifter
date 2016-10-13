namespace Shapeshifter.WindowsDesktop.Services.Web.Updates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Files.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Octokit;

    using Processes.Interfaces;

    using Web.Interfaces;
    using Infrastructure.Environment.Interfaces;

    [TestClass]
    public class UpdateServiceTest: UnitTestFor<IUpdateService>
    {
        [TestMethod]
        public async Task UpdateNotFoundIfNoLaterVersionFound()
        {
            var fakeReleasesClient = Substitute.For<IReleasesClient>();
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Repository
                .Release
                .Returns(fakeReleasesClient);

            Container.Resolve<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);

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
            
            await SystemUnderTest.UpdateAsync();

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
                .Repository
                .Release
                .Returns(fakeReleasesClient);

            Container.Resolve<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);

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
            
            await SystemUnderTest.UpdateAsync();

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
                .Repository
                .Release
                .Returns(fakeReleasesClient);

            Container.Resolve<IGitHubClientFactory>()
                     .CreateClient()
                     .Returns(fakeGitHubClient);

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
            
            await SystemUnderTest.UpdateAsync();

            fakeReleasesClient
                .DidNotReceive()
                .GetAllAssets(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<int>())
                .IgnoreAwait();
        }

        [TestMethod]
        public async Task UpdateFoundIfLaterVersionFound()
        {
            var fakeGitHubClient = Container
                .Resolve<IGitHubClientFactory>()
                .CreateClient();

            var fakeEnvironmentInformation = Container
                .Resolve<IEnvironmentInformation>();
            fakeEnvironmentInformation.GetHasInternetAccess().Returns(true);
            fakeEnvironmentInformation.GetShouldUpdate().Returns(true);

            fakeGitHubClient
                .Repository
                .Release
                .GetAll("ffMathy", "Shapeshifter")
                .Returns(
                    Task.FromResult<IReadOnlyList<Release>>(
                        new[]
                        {
                            CreateRelease(
                                1337,
                                "shapeshifter-v1337.0.0.0",
                                false,
                                false)
                        }));

            fakeGitHubClient
                .Repository
                .Release
                .GetAllAssets("ffMathy", "Shapeshifter", 1337)
                .Returns(
                    Task.FromResult<IReadOnlyList<ReleaseAsset>>(
                        new[]
                        {
                            CreateReleaseAsset("Shapeshifter.exe")
                        }));
            
            Container.Resolve<IFileManager>()
             .WriteBytesToTemporaryFile(
                 "Shapeshifter.exe",
                 Arg.Any<byte[]>())
             .Returns("temporaryInstallPath");
            
            await SystemUnderTest.UpdateAsync();

            var fakeDownloader = Container.Resolve<IDownloader>();
            fakeDownloader
                .Received()
                .DownloadBytesAsync("browserDownloadUrl")
                .IgnoreAwait();

            var fakeProcessManager = Container.Resolve<IProcessManager>();
            fakeProcessManager
                .Received()
                .LaunchFile("temporaryInstallPath", "update");
        }

        static ReleaseAsset CreateReleaseAsset(string name)
        {
            return new ReleaseAsset(
                "url",
                1,
                name,
                "label",
                "state",
                "contentType",
                2,
                3,
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                "browserDownloadUrl",
                new Author());
        }

        static Release CreateRelease(
            int id,
            string name,
            bool isPrerelease,
            bool isDraft)
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
                DateTimeOffset.Now,
                new Author(), 
                "foo",
                "bar",
                null);
        }
    }
}