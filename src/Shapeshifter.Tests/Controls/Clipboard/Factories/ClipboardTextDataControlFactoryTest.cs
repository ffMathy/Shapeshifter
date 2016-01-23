namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardTextDataControlFactoryTest: UnitTestFor<IClipboardTextDataControlFactory>
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            systemUnderTest.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForTextData()
        {
            Assert.IsTrue(systemUnderTest.CanBuildControl(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonTextData()
        {
            Assert.IsFalse(systemUnderTest.CanBuildControl(Substitute.For<IClipboardData>()));
        }
    }
}