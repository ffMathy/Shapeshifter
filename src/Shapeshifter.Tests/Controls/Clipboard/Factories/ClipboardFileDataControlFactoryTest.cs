namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClipboardFileDataControlFactoryTest: TestBase
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            var container = CreateContainer();

            var factory =
                container
                    .Resolve
                    <IClipboardDataControlFactory>();
            factory.BuildControl(null);
        }
    }
}