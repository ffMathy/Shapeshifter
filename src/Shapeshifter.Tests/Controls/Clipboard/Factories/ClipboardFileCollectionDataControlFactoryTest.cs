namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Interfaces;
    using Data.Interfaces;

    [TestClass]
    public class ClipboardFileCollectionDataControlFactoryTest: TestBase
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            var container = CreateContainer();

            var factory = container
                .Resolve<IClipboardFileCollectionDataControlFactory>();
            factory.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonFileData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardFileCollectionDataControlFactory>();
            Assert.IsFalse(factory.CanBuildControl(Substitute.For<IClipboardData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardFileCollectionDataControlFactory>();
            Assert.IsTrue(factory.CanBuildControl(Substitute.For<IClipboardFileCollectionData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileCollectionData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardFileCollectionDataControlFactory>();
            Assert.IsTrue(factory.CanBuildControl(Substitute.For<IClipboardFileCollectionData>()));
        }
    }
}