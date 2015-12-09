namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;
    using Services.Web.Interfaces;

    [TestClass]
    public class OpenLinkActionTest: ActionTestBase
    {
        [TestMethod]
        public void CanReadDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanReadTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(await action.CanPerformAsync(someNonTextData));
        }

        [TestMethod]
        public async Task CanPerformIsFalseForTextTypesWithNoLink()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<ILinkParser>()
                     .HasLinkAsync(Arg.Any<string>())
                     .Returns(Task.FromResult(false));
                });

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(await action.CanPerformAsync(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            var container = CreateContainer();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.AreEqual(200, action.Order);
        }

        [TestMethod]
        public async Task PerformLaunchesDefaultBrowsersForEachLink()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IProcessManager>();

                    c.RegisterFake<ILinkParser>()
                     .HasLinkAsync(Arg.Any<string>())
                     .Returns(Task.FromResult(true));

                    c.RegisterFake<ILinkParser>()
                     .ExtractLinksFromTextAsync(Arg.Any<string>())
                     .Returns(
                         Task
                             .FromResult
                             <IReadOnlyCollection<string>>(
                                 new[]
                                 {
                                     "foo.com",
                                     "bar.com"
                                 }));
                });

            var action = container.Resolve<IOpenLinkAction>();
            await action.PerformAsync(GetPackageContaining<IClipboardTextData>());

            var fakeProcessManager = container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1)
                              .LaunchCommand("foo.com");
            fakeProcessManager.Received(1)
                              .LaunchCommand("bar.com");
        }
    }
}