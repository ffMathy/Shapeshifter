namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;

    [TestClass]
    public class PasteActionTest: ActionTestBase
    {
        [TestMethod]
        public async Task CanAlwaysPerformIfDataIsGiven()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IPasteAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanGetTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanGetDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public async Task PerformTriggersPaste()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IClipboardInjectionService>();
                    c.RegisterFake<IClipboardPasteService>();
                });

            var fakeData = GetPackageContaining<IClipboardData>();

            var action = container.Resolve<IPasteAction>();
            await action.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService
                .Received()
                .InjectData(fakeData);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public async Task ThrowsExceptionIfNoDataGiven()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAction>();
            await action.CanPerformAsync(null);
        }
    }
}