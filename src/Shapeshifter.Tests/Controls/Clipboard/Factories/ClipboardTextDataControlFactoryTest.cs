namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
    public class ClipboardTextDataControlFactoryTest: UnitTestFor<IClipboardTextDataControlFactory>
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            SystemUnderTest.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForTextData()
        {
            Assert.IsTrue(SystemUnderTest.CanBuildControl(CreateClipboardDataPackageContaining<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonTextData()
        {
            Assert.IsFalse(SystemUnderTest.CanBuildControl(CreateClipboardDataPackageContaining<IClipboardData>()));
        }
    }
}