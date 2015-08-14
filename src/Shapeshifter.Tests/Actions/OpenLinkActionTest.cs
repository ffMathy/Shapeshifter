using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class OpenLinkActionTest : TestBase
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
        public void CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(action.CanPerform(someNonTextData));
        }

        [TestMethod]
        public void CanPerformIsFalseForTextTypesWithNoLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                    .HasLink(Arg.Any<string>())
                    .Returns(false);
            });

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(action.CanPerform(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public async Task PerformLaunchesDefaultBrowsersForEachLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IProcessManager>();

                c.RegisterFake<ILinkParser>()
                    .ExtractLinksFromText(Arg.Any<string>())
                    .Returns(new[] { "foo.com", "bar.com" });
            });

            var fakeData = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IOpenLinkAction>();
            await action.PerformAsync(fakeData);

            var fakeProcessManager = container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1)
                .StartProcess("foo.com", null);
            fakeProcessManager.Received(1)
                .StartProcess("bar.com", null);
        }
    }
}
