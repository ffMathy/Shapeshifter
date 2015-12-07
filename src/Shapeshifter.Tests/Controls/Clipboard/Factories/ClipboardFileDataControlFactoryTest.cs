namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Autofac;

    using Clipboard.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;
    using Data.Interfaces;

    using NSubstitute;

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