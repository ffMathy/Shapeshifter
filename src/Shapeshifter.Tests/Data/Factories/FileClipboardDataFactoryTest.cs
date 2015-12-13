namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;

    using Api;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileClipboardDataFactoryTest : TestBase
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForSingleFileFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataFactory>();
            Assert.IsTrue(factory.CanBuildData(ClipboardApi.CF_HDROP));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonFileFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataFactory>();
            Assert.IsFalse(factory.CanBuildData(uint.MaxValue));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BuildDataForIncompatibleFormatThrowsException()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataFactory>();
            factory.BuildData(uint.MaxValue, new byte[0]);
        }

        [TestMethod]
        public void CanBuildDataReturnsTrueForFileDropFormat()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataFactory>();
            Assert.IsTrue(factory.CanBuildData(ClipboardApi.CF_HDROP));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForInvalidFormat()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataFactory>();
            Assert.IsFalse(factory.CanBuildData(uint.MaxValue));
        }
    }
}
