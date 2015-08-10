using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class PasteAsPlainTextActionTest : TestBase
    {
        [TestMethod]
        public void CanNotPerformWithNonTextData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsFalse(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithTextData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IPasteAsPlainTextAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }
    }
}
