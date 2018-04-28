namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardFileCollectionDataControlFactoryTest: UnitTestFor<IClipboardFileCollectionDataControlFactory>
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            SystemUnderTest.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonFileData()
        {
            Assert.IsFalse(SystemUnderTest.CanBuildControl(Substitute.For<IClipboardDataPackage>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileData()
        {
            Assert.IsTrue(SystemUnderTest.CanBuildControl(CreateClipboardDataPackageContaining<IClipboardFileCollectionData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileCollectionData()
        {
            Assert.IsTrue(SystemUnderTest.CanBuildControl(CreateClipboardDataPackageContaining<IClipboardFileCollectionData>()));
        }
    }
}