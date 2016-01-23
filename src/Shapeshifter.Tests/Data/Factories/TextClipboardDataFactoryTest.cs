namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    [TestClass]
    public class TextClipboardDataFactoryTest: UnitTestFor<ITextClipboardDataFactory>
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForTextFormats()
        {
            Assert.IsTrue(
                systemUnderTest.CanBuildData(
                    ClipboardNativeApi.CF_TEXT));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            Assert.IsFalse(
                systemUnderTest.CanBuildData(uint.MaxValue));
        }

        [TestMethod]
        public void BuildDataReturnsTextData()
        {
            var data = systemUnderTest.BuildData(
                ClipboardNativeApi.CF_TEXT, new byte[0]);
            Assert.IsInstanceOfType(data, typeof (ClipboardTextData));
        }
    }
}