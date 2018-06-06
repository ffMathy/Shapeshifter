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
        public void OrderIsCorrect()
        {
            Assert.AreEqual(byte.MaxValue, SystemUnderTest.Order);
        }
    }
}