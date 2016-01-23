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
            Assert.IsTrue(systemUnderTest.CanBuildData(ClipboardNativeApi.CF_HDROP));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonFileFormats()
        {
            Assert.IsFalse(systemUnderTest.CanBuildData(uint.MaxValue));
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void BuildDataForIncompatibleFormatThrowsException()
        {
            systemUnderTest.BuildData(uint.MaxValue, new byte[0]);
        }

        [TestMethod]
        public void CanBuildDataReturnsTrueForFileDropFormat()
        {
            Assert.IsTrue(systemUnderTest.CanBuildData(ClipboardNativeApi.CF_HDROP));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForInvalidFormat()
        {
            Assert.IsFalse(systemUnderTest.CanBuildData(uint.MaxValue));
        }
    }
}