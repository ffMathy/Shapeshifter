using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class PasteAsPlainTextActionTest : TestBase
    {
        [TestMethod]
        public void CanNotPerformWithNonTextData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsFalse(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithTextData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanGetDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public async Task PerformCausesTextOfDataToBeCopied()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardInjectionService>();
            });

            var fakeData = Substitute.For<IClipboardTextData>();
            fakeData.Text.Returns("foobar hello");

            var action = container.Resolve<IPasteAsPlainTextAction>();
            await action.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(1)
                .InjectText("foobar hello");
        }
    }
}
