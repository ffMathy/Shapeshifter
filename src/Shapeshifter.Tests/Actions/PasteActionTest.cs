using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class PasteActionTest : TestBase
    {
        [TestMethod]
        public void CanAlwaysPerformIfDataIsGiven()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanGetTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanGetDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IPasteAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public async Task PerformTriggersPaste()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardInjectionService>();
            });

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAction>();
            await action.PerformAsync(fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.InjectData(fakeData);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsExceptionIfNoDataGiven()
        {
            var container = CreateContainer();
            
            var action = container.Resolve<IPasteAction>();
            action.CanPerform(null);
        }
    }
}
