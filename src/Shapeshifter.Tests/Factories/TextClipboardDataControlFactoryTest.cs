using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.Tests.Factories
{
    [TestClass]
    public class TextClipboardDataControlFactoryTest : TestBase
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataControlFactory>();
            Assert.IsTrue(factory.CanBuildData("Text"));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildData("foobar"));
        }
    }
}
