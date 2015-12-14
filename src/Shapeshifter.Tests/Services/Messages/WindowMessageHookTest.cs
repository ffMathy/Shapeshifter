namespace Shapeshifter.WindowsDesktop.Services.Messages
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

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
    }
}