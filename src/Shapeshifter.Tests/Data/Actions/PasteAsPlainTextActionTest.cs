namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;

    [TestClass]
    public class PasteAsPlainTextActionTest: ActionTestBase<IPasteAsPlainTextAction>
    {
        [TestMethod]
        public async Task CanNotPerformWithNonTextData()
        {
            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithTextData()
        {
            var fakeData = GetPackageContaining<IClipboardTextData>();
            Assert.IsTrue(
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanGetDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(25, systemUnderTest.Order);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public async Task PerformCausesTextOfDataToBeCopied()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("foobar hello");

            var fakeData = GetPackageContaining(fakeTextData);
            
            await systemUnderTest.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(1)
                                         .InjectText("foobar hello");
        }
    }
}