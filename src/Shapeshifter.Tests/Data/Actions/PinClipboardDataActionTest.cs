namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Files;
    using Services.Files.Interfaces;

    [TestClass]
    public class PinClipboardDataActionTest : ActionTestBase
    {
        [TestMethod]
        public async Task CanNotPerformWithEmptyData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IPinClipboardDataAction>();
            Assert.IsFalse(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPinClipboardDataAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPinClipboardDataAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPinClipboardDataAction>();
            Assert.AreEqual(200, action.Order);
        }
    }
}