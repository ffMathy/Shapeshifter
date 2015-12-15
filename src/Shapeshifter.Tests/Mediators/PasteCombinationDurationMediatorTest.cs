namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Autofac;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    using NSubstitute;

    using Services.Keyboard.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class PasteCombinationDurationMediatorTest : TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfConsumerThreadIsRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        public void CombinationIsHeldDownWhenCtrlVIsDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyCtrlIsDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyVIsDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenBothVAndCtrlIsReleased()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyVIsDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyCtrlIsDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenBothVAndCtrlAreDown()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationReleasedWhenBothVAndCtrlAreReleased()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Connect(Substitute.For<IWindow>());
        }

        [TestMethod]
        public void WhenCtrlVIsDownMonitoringLoopRuns()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IPasteHotkeyInterceptor>();
                    c.RegisterFake<IConsumerThreadLoop>();
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Connect(Substitute.For<IWindow>());

            var fakePasteHotkeyInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            var fakeConsumerThreadLoop = container.Resolve<IConsumerThreadLoop>();
            fakeConsumerThreadLoop
                .Received()
                .Notify(
                    Arg.Any<Func<Task>>(),
                    Arg.Any<CancellationToken>());
        }

        static void RaiseHotkeyFired(IPasteHotkeyInterceptor fakePasteHotkeyInterceptor)
        {
            fakePasteHotkeyInterceptor.HotkeyFired += Raise.EventWith(
                            fakePasteHotkeyInterceptor,
                            new HotkeyFiredArgument(
                                KeyboardNativeApi.VK_KEY_V,
                                true));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
        public void WhenCombinationIsDownForLongEnoughThenEventFires()
        {
            var decisecondsPassed = 0;
            var holdCombinationDown = true;

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IThreadDelay>()
                     .ExecuteAsync(Arg.Do<int>(
                         x => decisecondsPassed += x/100))
                     .Returns(Task.CompletedTask);
                    c.RegisterFake<IPasteHotkeyInterceptor>();
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x =>
                         {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(i => holdCombinationDown);
                             x.IsKeyDown(Key.V)
                              .Returns(i => holdCombinationDown);
                         });
                    c.RegisterFake<IConsumerThreadLoop>()
                     .Notify(
                         Arg.Do<Func<Task>>(async x => await x()),
                         Arg.Any<CancellationToken>());
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.PasteCombinationDurationPassed += (sender, e) => holdCombinationDown = false;
            mediator.Connect(Substitute.For<IWindow>());

            var fakePasteHotkeyInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            Assert.AreEqual(mediator.DurationInDeciseconds, decisecondsPassed);
        }
    }
}