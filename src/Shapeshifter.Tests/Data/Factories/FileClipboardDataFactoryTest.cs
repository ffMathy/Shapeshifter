namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    [TestClass]
    public class FileClipboardDataFactoryTest: UnitTestFor<IFileClipboardDataFactory>
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForSingleFileFormats()
        {
            Assert.IsTrue(SystemUnderTest.CanBuildData(CreateClipboardFormatFromNumber(ClipboardNativeApi.CF_HDROP)));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonFileFormats()
        {
            Assert.IsFalse(SystemUnderTest.CanBuildData(CreateClipboardFormatFromNumber(uint.MaxValue)));
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void BuildDataForIncompatibleFormatThrowsException()
        {
            SystemUnderTest.BuildData(CreateClipboardFormatFromNumber(uint.MaxValue), new byte[0]);
        }

        [TestMethod]
        public void CanBuildDataReturnsTrueForFileDropFormat()
        {
            Assert.IsTrue(SystemUnderTest.CanBuildData(CreateClipboardFormatFromNumber(ClipboardNativeApi.CF_HDROP)));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForInvalidFormat()
        {
            Assert.IsFalse(SystemUnderTest.CanBuildData(CreateClipboardFormatFromNumber(uint.MaxValue)));
        }
    }
}