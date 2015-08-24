using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardData>();

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

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardTextData>();

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
                    .ExtractLinksFromTextAsync(Arg.Any<string>())
                    .Returns(Task.FromResult<IEnumerable<string>>(new[] { "foo.com", "bar.com" }));
            });

            var fakeData = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IOpenLinkAction>();
            await action.PerformAsync(fakeData, null);

            var fakeProcessManager = container.Resolve<IProcessManager>();
            fakeProcessManager.Received(1)
                .StartProcess("foo.com", null);
            fakeProcessManager.Received(1)
                .StartProcess("bar.com", null);
        }
    }
}
