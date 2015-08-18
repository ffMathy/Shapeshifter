using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;

namespace Shapeshifter.Tests.Mediators
{
    [TestClass]
    public class ClipboardUserInterfaceMediatorTest : TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfClipboardHookIsNotConnected()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(false);

                c.RegisterFake<IKeyboardHookService>()
                    .IsConnected
                    .Returns(true);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsFalseIfKeyboardHookIsNotConnected()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(true);

                c.RegisterFake<IKeyboardHookService>()
                    .IsConnected
                    .Returns(false);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfAllHooksAreConnected()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(true);

                c.RegisterFake<IKeyboardHookService>()
                    .IsConnected
                    .Returns(true);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        public void ConnectConnectsKeyboardHook()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ConnectConnectsClipboardHook()
        {
            throw new NotImplementedException();
        }
    }
}
