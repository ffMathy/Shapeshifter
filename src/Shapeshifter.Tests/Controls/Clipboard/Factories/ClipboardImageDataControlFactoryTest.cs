using System;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard.Factories
{
    [TestClass]
    public class ClipboardImageDataControlFactoryTest : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            var container = CreateContainer();

            var factory =
                container
                    .Resolve
                    <IClipboardControlFactory<IClipboardImageData, IClipboardImageDataControl>>();
            factory.CreateControl(null);
        }
    }
}