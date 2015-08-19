using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard.Factories
{
    [TestClass]
    public class ClipboardTextDataControlFactoryTest : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>>();
            factory.CreateControl(null);
        }
    }
}
