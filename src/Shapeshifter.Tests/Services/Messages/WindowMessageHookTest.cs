namespace Shapeshifter.WindowsDesktop.Services.Messages
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Interop;

    using Autofac;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class WindowMessageHookTest: UnitTestFor<IWindowMessageHook>
    {
        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectWhenAlreadyDisconnectedThrowsException()
        {
            systemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectWhenAlreadyConnectedThrowsException()
        {
            var fakeWindow = Substitute.For<IHookableWindow>();
            
            systemUnderTest.Connect(fakeWindow);
            systemUnderTest.Connect(fakeWindow);
        }

        [TestMethod]
        public void ReceivingTwoMessagesNotifiesConsumerTwice()
        {
            HwndSourceHook windowHookCallback = null;
            var fakeWindow = Substitute
                .For<IHookableWindow>()
                .With(
                    x => {
                        x.AddHwndSourceHook(
                            Arg.Do<HwndSourceHook>(
                                h => windowHookCallback = h));
                    });

            systemUnderTest.Connect(fakeWindow);

            Assert.IsNotNull(windowHookCallback);

            var handled = false;
            windowHookCallback(IntPtr.Zero, (int) Message.WM_HOTKEY, IntPtr.Zero, IntPtr.Zero, ref handled);
            windowHookCallback(IntPtr.Zero, (int) Message.WM_HOTKEY, IntPtr.Zero, IntPtr.Zero, ref handled);

            var fakeConsumerLoop = container.Resolve<IConsumerThreadLoop>();
            fakeConsumerLoop
                .Received(2)
                .Notify(
                    Arg.Any<Func<Task>>(),
                    Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public void ReceivingMessageGetsParsedOnToInterceptors()
        {
            container.Resolve<IConsumerThreadLoop>()
             .Notify(
                 Arg.Do<Func<Task>>(
                     async x => await x()),
                 Arg.Any<CancellationToken>());

            HwndSourceHook windowHookCallback = null;
            var fakeWindow = Substitute.For<IHookableWindow>()
                                       .With(
                                           x => {
                                               x.AddHwndSourceHook(
                                                   Arg.Do<HwndSourceHook>(
                                                       h => windowHookCallback = h));
                                           });

            systemUnderTest.Connect(fakeWindow);

            Assert.IsNotNull(windowHookCallback);

            var hwnd = new IntPtr(1);
            var wParam = new IntPtr(2);
            var lParam = new IntPtr(3);
            var handled = false;
            windowHookCallback(hwnd, (int) Message.WM_HOTKEY, wParam, lParam, ref handled);

            var fakeInterceptor = container.Resolve<IWindowMessageInterceptor>();
            fakeInterceptor
                .Received()
                .ReceiveMessageEvent(
                    Arg.Is<WindowMessageReceivedArgument>(
                        x => (x.LongParameter == lParam) &&
                             (x.WordParameter == wParam) &&
                             (x.WindowHandle == hwnd) &&
                             (x.Message == Message.WM_HOTKEY)));
        }
    }
}