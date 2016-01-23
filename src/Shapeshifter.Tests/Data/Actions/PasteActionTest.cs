namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;

    [TestClass]
    public class PasteActionTest: ActionTestBase<IPasteAction>
    {
        [TestMethod]
        public async Task CanAlwaysPerformIfDataIsGiven()
        {
            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsTrue(
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(0, systemUnderTest.Order);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public void CanGetDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public async Task PerformTriggersPaste()
        {
            var fakeData = GetPackageContaining<IClipboardData>();
            await systemUnderTest.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService
                .Received()
                .InjectData(fakeData);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public async Task ThrowsExceptionIfNoDataGiven()
        {
            await systemUnderTest.CanPerformAsync(null);
        }
    }
}