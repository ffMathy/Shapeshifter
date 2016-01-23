namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Autofac;

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
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(byte.MaxValue, systemUnderTest.Order);
        }
    }
}