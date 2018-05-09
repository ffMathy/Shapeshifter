namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class PinClipboardDataActionTest: ActionTestBase<IPinClipboardDataAction>
    {
        [TestMethod]
        public async Task CanNotPerformWithEmptyData()
        {
            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanReadDescription()
        {
            Assert.IsNotNull(await SystemUnderTest.GetTitleAsync(Substitute.For<IClipboardDataPackage>()));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(byte.MaxValue, SystemUnderTest.Order);
        }
    }
}