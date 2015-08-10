using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class PasteAsPlainTextActionTest : TestBase
    {
        [TestMethod]
        public void CanAlwaysPerformIfDataIsGiven()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsExceptionIfNoDataGiven()
        {
            var container = CreateContainer();
            
            var action = container.Resolve<IPasteAsPlainTextAction>();
            action.CanPerform(null);
        }
    }
}
