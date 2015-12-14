namespace Shapeshifter.WindowsDesktop.Services.Messages
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    using Controls.Window.Interfaces;

    [TestClass]
    public class WindowMessageHookTest: TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DisconnectWhenAlreadyDisconnectedThrowsException()
        {
            var container = CreateContainer();

            var hook = container.Resolve<IWindowMessageHook>();
            hook.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConnectWhenAlreadyConnectedThrowsException()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IWindowMessageInterceptor>();
                });

            var fakeWindow = Substitute.For<IWindow>();

            var hook = container.Resolve<IWindowMessageHook>();
            hook.Connect(fakeWindow);
            hook.Connect(fakeWindow);
        }
    }
}