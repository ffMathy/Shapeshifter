namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Windows.Input;

    using Autofac;

    using Factories.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class KeyInterceptorTest: UnitTestFor<IKeyInterceptor>
    {
        public KeyInterceptorTest()
        {
            ExcludeFakeFor<IUserInterfaceThread>();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void InstallWhenAlreadyInstalledThrowsException()
        {
            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.Install(IntPtr.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void UninstallWhenNotInstalledThrowsException()
        {
            systemUnderTest.Uninstall();
        }

        [TestMethod]
        public void InstallStartsInterceptions()
        {
            var fakeInterception1 = Substitute.For<IHotkeyInterception>();
            var fakeInterception2 = Substitute.For<IHotkeyInterception>();

            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception1, fakeInterception2);
            
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            systemUnderTest.Install(IntPtr.Zero);

            fakeInterception1.Received()
                             .Start(IntPtr.Zero);
            fakeInterception2.Received()
                             .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void ReceivingWindowShownEventInstallsHotkeys()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);
            
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    IntPtr.Zero,
                    Message.WM_SHOWWINDOW,
                    new IntPtr(1),
                    IntPtr.Zero));

            fakeInterception.Received()
                            .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void ReceivingWindowHiddenEventUninstallsHotkeys()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();
            
            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.Install(IntPtr.Zero);

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    IntPtr.Zero,
                    Message.WM_SHOWWINDOW,
                    new IntPtr(0),
                    IntPtr.Zero));

            fakeInterception.Received()
                            .Stop(IntPtr.Zero);
        }

        [TestMethod]
        public void ReceivingHotkeyEventWithInstalledHotkeyTriggersEvent()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();
            fakeInterception.InterceptionId.Returns(1337);
            fakeInterception.Key.Returns(Key.B);

            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);
            
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.Install(IntPtr.Zero);

            var eventFired = false;
            systemUnderTest.HotkeyFired += (sender, e) => {
                eventFired = e.Key == Key.B;
            };

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    IntPtr.Zero,
                    Message.WM_HOTKEY,
                    new IntPtr(1337),
                    IntPtr.Zero));

            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void UninstallStopsInterceptions()
        {
            var fakeInterception1 = Substitute.For<IHotkeyInterception>();
            var fakeInterception2 = Substitute.For<IHotkeyInterception>();


            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception1, fakeInterception2);
            
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.Uninstall();

            fakeInterception1.Received()
                             .Stop(IntPtr.Zero);
            fakeInterception2.Received()
                             .Stop(IntPtr.Zero);
        }

        [TestMethod]
        public void AddInterceptingKeyCreatesInterception()
        {
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            var fakeFactory = container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received()
                       .CreateInterception(Key.A, true, false);
        }

        [TestMethod]
        public void AddInterceptingKeyOnInstalledInterceptorInstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.A, true, false)
             .Returns(fakeInterception);
            
            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            fakeInterception.Received()
                            .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void RemoveInterceptingKeyWithNoKeysDoesNotDoAnything()
        {
            systemUnderTest.RemoveInterceptingKey(IntPtr.Zero, Key.A);
        }

        [TestMethod]
        public void AddInterceptingKeyWithKeyAlreadyThereDoesNotDoAnything()
        {
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            var fakeFactory = container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received(1)
                       .CreateInterception(Key.A, true, false);
        }

        [TestMethod]
        public void RemoveInterceptingKeyOnInstalledInterceptorUninstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.A, true, false)
             .Returns(fakeInterception);
            
            systemUnderTest.Install(IntPtr.Zero);

            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.RemoveInterceptingKey(IntPtr.Zero, Key.A);

            fakeInterception.Received()
                            .Stop(IntPtr.Zero);
        }
    }
}