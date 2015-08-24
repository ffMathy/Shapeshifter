using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using System.Windows;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class PasteActionTest : TestBase
    {
        [TestMethod]
        public async Task CanAlwaysPerformIfDataIsGiven()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IPasteAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
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

            var fakeData = Substitute.For<IDataObject>();

            var action = container.Resolve<IPasteAction>();
            await action.PerformAsync(null, fakeData);

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.InjectData(fakeData);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ThrowsExceptionIfNoDataGiven()
        {
            var container = CreateContainer();
            
            var action = container.Resolve<IPasteAction>();
            await action.CanPerformAsync(null);
        }
    }
}
