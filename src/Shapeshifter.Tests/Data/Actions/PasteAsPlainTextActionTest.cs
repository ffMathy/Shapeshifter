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
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithTextData()
        {
            var fakeData = GetPackageContaining<IClipboardTextData>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanReadDescription()
        {
            Assert.IsNotNull(await SystemUnderTest.GetDescriptionAsync(Substitute.For<IClipboardDataPackage>()));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(25, SystemUnderTest.Order);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            Assert.IsNotNull(SystemUnderTest.Title);
        }

        [TestMethod]
        public async Task PerformCausesTextOfDataToBeCopied()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("foobar hello");

            var fakeData = GetPackageContaining(fakeTextData);
            
            await SystemUnderTest.PerformAsync(fakeData);

            var fakeClipboardInjectionService = Container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(1)
                                         .InjectTextAsync("foobar hello")
                                         .IgnoreAwait();
        }
    }
}