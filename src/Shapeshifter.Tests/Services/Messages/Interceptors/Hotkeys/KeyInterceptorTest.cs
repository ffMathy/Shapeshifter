namespace Shapeshifter.Tests.Services.Messages.Interceptors.Hotkeys
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using UserInterface.WindowsDesktop.Api;
    using UserInterface.WindowsDesktop.Infrastructure.Events;
    using UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces;
    using UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class KeyInterceptorTest: TestBase
    {
        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void InstallWhenAlreadyInstalledThrowsException()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.Install(IntPtr.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void UninstallWhenNotInstalledThrowsException()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.Uninstall();
        }

        [TestMethod]
        public void InstallStartsInterceptions()
        {
            var fakeInterception1 = Substitute.For<IHotkeyInterception>();
            var fakeInterception2 = Substitute.For<IHotkeyInterception>();

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Arg.Any<int>(), true, false)
                     .Returns(fakeInterception1, fakeInterception2);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 2);

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

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Arg.Any<int>(), true, false)
                     .Returns(fakeInterception);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);

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

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Arg.Any<int>(), true, false)
                     .Returns(fakeInterception);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);
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
            fakeInterception.KeyCode.Returns(1338);

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Arg.Any<int>(), true, false)
                     .Returns(fakeInterception);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);
            systemUnderTest.Install(IntPtr.Zero);

            var eventFired = false;
            systemUnderTest.HotkeyFired += (sender, e) =>
            {
                eventFired = e.KeyCode == 1338;
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

            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Arg.Any<int>(), true, false)
                     .Returns(fakeInterception1, fakeInterception2);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 2);

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
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>();
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);

            var fakeFactory = container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received()
                       .CreateInterception(1337, true, false);
        }

        [TestMethod]
        public void AddInterceptingKeyOnInstalledInterceptorInstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(1337, true, false)
                     .Returns(fakeInterception);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);

            fakeInterception.Received()
                            .Start(IntPtr.Zero);
        }

        [TestMethod]
        public void RemoveInterceptingKeyWithNoKeysDoesNotDoAnything()
        {
            var container = CreateContainer();

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.RemoveInterceptingKey(IntPtr.Zero, 1337);
        }

        [TestMethod]
        public void AddInterceptingKeyWithKeyAlreadyThereDoesNotDoAnything()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>();
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);
            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);

            var fakeFactory = container.Resolve<IHotkeyInterceptionFactory>();
            fakeFactory.Received(1)
                       .CreateInterception(1337, true, false);
        }

        [TestMethod]
        public void RemoveInterceptingKeyOnInstalledInterceptorUninstallsInterception()
        {
            var fakeInterception = Substitute.For<IHotkeyInterception>();
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(1337, true, false)
                     .Returns(fakeInterception);
                });

            var systemUnderTest = container.Resolve<IKeyInterceptor>();
            systemUnderTest.Install(IntPtr.Zero);

            systemUnderTest.AddInterceptingKey(IntPtr.Zero, 1337);
            systemUnderTest.RemoveInterceptingKey(IntPtr.Zero, 1337);

            fakeInterception.Received()
                            .Stop(IntPtr.Zero);
        }
    }
}