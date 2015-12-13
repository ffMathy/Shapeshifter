namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using Autofac;

    using Data;
    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TextClipboardDataFactoryTest : TestBase
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataFactory>();
            Assert.IsTrue(factory.CanBuildData(ClipboardApi.CF_TEXT));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataFactory>();
            Assert.IsFalse(factory.CanBuildData(uint.MaxValue));
        }

        [TestMethod]
        public void BuildDataReturnsTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataFactory>();
            var data = factory.BuildData(ClipboardApi.CF_TEXT, new byte[0]);

            Assert.IsInstanceOfType(data, typeof(ClipboardTextData));
        }
    }
}
