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
            SystemUnderTest.Install(IntPtr.Zero);
            SystemUnderTest.Install(IntPtr.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void UninstallWhenNotInstalledThrowsException()
        {
            SystemUnderTest.Uninstall();
        }

        [TestMethod]
        public void InstallStartsInterceptions()
        {
            var fakeInterception1 = Substitute.For<IHotkeyInterception>();
            var fakeInterception2 = Substitute.For<IHotkeyInterception>();

            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception1, fakeInterception2);
            
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            SystemUnderTest.Install(IntPtr.Zero);

            fakeInterception1.Received()
                             .Start(IntPtr.Zero);
            fakeInterception2.Received()
                             .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void ReceivingWindowShownEventInstallsHotkeys()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);
            
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            SystemUnderTest.ReceiveMessageEventAsync(
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
            
            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);

            var systemUnderTest = Container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            systemUnderTest.Install(IntPtr.Zero);

            systemUnderTest.ReceiveMessageEventAsync(
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

            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception);
            
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            SystemUnderTest.Install(IntPtr.Zero);

            var eventFired = false;
            SystemUnderTest.HotkeyFired += (sender, e) => {
                eventFired = e.Key == Key.B;
            };

            SystemUnderTest.ReceiveMessageEventAsync(
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


            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Arg.Any<Key>(), true, false)
             .Returns(fakeInterception1, fakeInterception2);
            
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            SystemUnderTest.Install(IntPtr.Zero);
            SystemUnderTest.Uninstall();

            fakeInterception1.Received()
                             .Stop(IntPtr.Zero);
            fakeInterception2.Received()
                             .Stop(IntPtr.Zero);
        }

        [TestMethod]
        public void AddInterceptingKeyCreatesInterception()
        {
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            var fakeFactory = Container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received()
                       .CreateInterception(Key.A, true, false);
        }

        [TestMethod]
        public void AddInterceptingKeyOnInstalledInterceptorInstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.A, true, false)
             .Returns(fakeInterception);
            
            SystemUnderTest.Install(IntPtr.Zero);
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);

            fakeInterception.Received()
                            .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void RemoveInterceptingKeyWithNoKeysDoesNotDoAnything()
        {
            SystemUnderTest.RemoveInterceptingKey(IntPtr.Zero, Key.A);
        }

        [TestMethod]
        public void AddInterceptingKeyWithKeyAlreadyThereDoesNotDoAnything()
        {
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.B);

            var fakeFactory = Container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received(1)
                       .CreateInterception(Key.A, true, false);
        }

        [TestMethod]
        public void RemoveInterceptingKeyOnInstalledInterceptorUninstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();

            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.A, true, false)
             .Returns(fakeInterception);
            
            SystemUnderTest.Install(IntPtr.Zero);

            SystemUnderTest.AddInterceptingKey(IntPtr.Zero, Key.A);
            SystemUnderTest.RemoveInterceptingKey(IntPtr.Zero, Key.A);

            fakeInterception.Received()
                            .Stop(IntPtr.Zero);
        }
    }
}