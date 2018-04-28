namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Processes.Interfaces;
    using Services.Web.Interfaces;

    [TestClass]
    public class OpenLinkActionTest: ActionTestBase<IOpenLinkAction>
    {

        [TestMethod]
        public async Task CanReadDescription()
        {
            FakeHasLinks(
                new[]
                {
                    "foo.com",
                    "bar.com"
                });

            Assert.IsNotNull(await SystemUnderTest.GetDescriptionAsync(Substitute
                .For<IClipboardDataPackage>()
                .With(x => x.Contents.Returns(
                    new []
                    {
                        Substitute.For<IClipboardTextData>() 
                    }))));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(SystemUnderTest.Title);
        }

        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var someNonTextData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await SystemUnderTest.CanPerformAsync(
                    someNonTextData));
        }

        [TestMethod]
        public async Task CanPerformIsFalseForTextTypesWithNoLink()
        {
            Container.Resolve<ILinkParser>()
             .HasLinkAsync(Arg.Any<string>())
             .Returns(Task.FromResult(false));

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardDataPackage>();
            
            Assert.IsFalse(await SystemUnderTest.CanPerformAsync(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(200, SystemUnderTest.Order);
        }

        [TestMethod]
        public async Task PerformLaunchesDefaultBrowsersForEachLink()
        {
            ExcludeFakeFor<IAsyncFilter>();

            FakeHasLinks(
                new[]
                {
                    "foo.com",
                    "bar.com"
                });

            await SystemUnderTest.PerformAsync(
                CreateClipboardDataPackageContaining<IClipboardTextData>());

            var fakeProcessManager = Container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1)
                              .LaunchCommand("foo.com");
            fakeProcessManager.Received(1)
                              .LaunchCommand("bar.com");
        }

        void FakeHasLinks(string[] links)
        {
            Container.Resolve<ILinkParser>()
                     .HasLinkAsync(Arg.Any<string>())
                     .Returns(Task.FromResult(true));

            Container.Resolve<ILinkParser>()
                     .ExtractLinksFromTextAsync(Arg.Any<string>())
                     .Returns(
                         Task
                             .FromResult<IReadOnlyCollection<string>>(
                                 links));
        }
    }
}