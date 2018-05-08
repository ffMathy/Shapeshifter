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
    public class PasteActionTest : ActionTestBase<IPasteAction>
    {
        [TestMethod]
        public async Task CanAlwaysPerformIfDataIsGiven()
        {
            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(0, SystemUnderTest.Order);
        }

        [TestMethod]
        public async Task CanReadDescription()
        {
            Assert.IsNotNull(await SystemUnderTest.GetTitleAsync(Substitute.For<IClipboardDataPackage>()));
        }

        [TestMethod]
        public async Task PerformTriggersPaste()
        {
            var fakeData = CreateClipboardDataPackageContaining<IClipboardData>();
            await SystemUnderTest.PerformAsync(fakeData);

            var fakeClipboardInjectionService = Container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService
                .Received()
                .InjectDataAsync(fakeData)
                .IgnoreAwait();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ThrowsExceptionIfNoDataGiven()
        {
            await SystemUnderTest.CanPerformAsync(null);
        }
    }
}