namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileTypeInterpreterTest: UnitTestFor<IFileTypeInterpreter>
    {
        [TestMethod]
        public void CanGetPngFileType()
        {
            Assert.AreEqual(
                FileType.Image, 
                systemUnderTest.GetFileTypeFromFileName("foo.png"));
        }

        [TestMethod]
        public void CanGetJpgFileType()
        {
            Assert.AreEqual(
                FileType.Image,
                systemUnderTest.GetFileTypeFromFileName("foo.jpg"));
        }

        [TestMethod]
        public void CanGetTxtFileType()
        {
            Assert.AreEqual(
                FileType.Text,
                systemUnderTest.GetFileTypeFromFileName("foo.txt"));
        }

        [TestMethod]
        public void GetsOtherTypeWhenNoFileExtension()
        {
            Assert.AreEqual(
                FileType.Other,
                systemUnderTest.GetFileTypeFromFileName("foo"));
        }

        [TestMethod]
        public void GetsOtherTypeWhenOtherFileExtension()
        {
            Assert.AreEqual(
                FileType.Other,
                systemUnderTest.GetFileTypeFromFileName("foo.other"));
        }
    }
}