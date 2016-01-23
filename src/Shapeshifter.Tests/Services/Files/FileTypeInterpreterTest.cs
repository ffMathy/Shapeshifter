namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileTypeInterpreterTest: TestBase
    {
        [TestMethod]
        public void CanGetPngFileType()
        {
            var container = CreateContainer();

            var fileTypeInterpreter = container.Resolve<IFileTypeInterpreter>();
            Assert.AreEqual(FileType.Image, fileTypeInterpreter.GetFileTypeFromFileName("foo.png"));
        }

        [TestMethod]
        public void CanGetJpgFileType()
        {
            var container = CreateContainer();

            var fileTypeInterpreter = container.Resolve<IFileTypeInterpreter>();
            Assert.AreEqual(FileType.Image, fileTypeInterpreter.GetFileTypeFromFileName("foo.jpg"));
        }

        [TestMethod]
        public void CanGetTxtFileType()
        {
            var container = CreateContainer();

            var fileTypeInterpreter = container.Resolve<IFileTypeInterpreter>();
            Assert.AreEqual(FileType.Text, fileTypeInterpreter.GetFileTypeFromFileName("foo.txt"));
        }

        [TestMethod]
        public void GetsOtherTypeWhenNoFileExtension()
        {
            var container = CreateContainer();

            var fileTypeInterpreter = container.Resolve<IFileTypeInterpreter>();
            Assert.AreEqual(FileType.Other, fileTypeInterpreter.GetFileTypeFromFileName("foo"));
        }

        [TestMethod]
        public void GetsOtherTypeWhenOtherFileExtension()
        {
            var container = CreateContainer();

            var fileTypeInterpreter = container.Resolve<IFileTypeInterpreter>();
            Assert.AreEqual(
                FileType.Other,
                fileTypeInterpreter.GetFileTypeFromFileName("foo.other"));
        }
    }
}