namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClipboardFileDataControlFactoryTest: UnitTestFor<IClipboardFileDataControlFactory>
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            systemUnderTest.BuildControl(null);
        }
    }
}