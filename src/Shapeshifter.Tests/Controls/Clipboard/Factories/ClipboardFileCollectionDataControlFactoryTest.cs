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
            systemUnderTest.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonFileData()
        {
            Assert.IsFalse(systemUnderTest.CanBuildControl(Substitute.For<IClipboardData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileData()
        {
            Assert.IsTrue(systemUnderTest.CanBuildControl(Substitute.For<IClipboardFileCollectionData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForFileCollectionData()
        {
            Assert.IsTrue(systemUnderTest.CanBuildControl(Substitute.For<IClipboardFileCollectionData>()));
        }
    }
}