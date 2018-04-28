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
					CreateClipboardFormatFromNumber(ClipboardNativeApi.CF_TEXT)));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            Assert.IsFalse(
                SystemUnderTest.CanBuildData(CreateClipboardFormatFromNumber(uint.MaxValue)));
        }

        [TestMethod]
        public void BuildDataReturnsTextData()
        {
            var data = SystemUnderTest.BuildData(
				CreateClipboardFormatFromNumber(ClipboardNativeApi.CF_TEXT), new byte[0]);
            Assert.IsInstanceOfType(data, typeof (ClipboardTextData));
        }
    }
}