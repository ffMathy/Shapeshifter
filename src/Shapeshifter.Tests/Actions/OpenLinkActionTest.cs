using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class OpenLinkActionTest : ActionTestBase
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
            var container = CreateContainer(c =>
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
        public async Task PerformLaunchesDefaultBrowsersForEachLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IProcessManager>();

                c.RegisterFake<ILinkParser>()
                    .HasLinkAsync(Arg.Any<string>())
                    .Returns(Task.FromResult(true));

                c.RegisterFake<ILinkParser>()
                    .ExtractLinksFromTextAsync(Arg.Any<string>())
                    .Returns(Task.FromResult<IEnumerable<string>>(new[] { "foo.com", "bar.com" }));
            });

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IOpenLinkAction>();
            await action.PerformAsync(GetPackageContaining<IClipboardTextData>());

            var fakeProcessManager = container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1).LaunchCommand("foo.com");
            fakeProcessManager.Received(1).LaunchCommand("bar.com");
        }
    }
}
