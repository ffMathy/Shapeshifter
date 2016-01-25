namespace Shapeshifter.WindowsDesktop.Data.Factories
{
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
                SystemUnderTest.CanBuildData(
                    ClipboardNativeApi.CF_TEXT));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            Assert.IsFalse(
                SystemUnderTest.CanBuildData(uint.MaxValue));
        }

        [TestMethod]
        public void BuildDataReturnsTextData()
        {
            var data = SystemUnderTest.BuildData(
                ClipboardNativeApi.CF_TEXT, new byte[0]);
            Assert.IsInstanceOfType(data, typeof (ClipboardTextData));
        }
    }
}