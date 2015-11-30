namespace Shapeshifter.UserInterface.WindowsDesktop.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using UserInterface.WindowsDesktop.Actions.Interfaces;
    using UserInterface.WindowsDesktop.Data.Interfaces;
    using UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

    [TestClass]
    public class PasteAsPlainTextActionTest: ActionTestBase
    {
        [TestMethod]
        public async Task CanNotPerformWithNonTextData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsFalse(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithTextData()
        {
            var container = CreateContainer();

            var fakeData = GetPackageContaining<IClipboardTextData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
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
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IClipboardInjectionService>();
                });

            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("foobar hello");

            var fakeData = GetPackageContaining(fakeTextData);

            var action = container.Resolve<IPasteAsPlainTextAction>();
            await action.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(1)
                                         .InjectText("foobar hello");
        }
    }
}