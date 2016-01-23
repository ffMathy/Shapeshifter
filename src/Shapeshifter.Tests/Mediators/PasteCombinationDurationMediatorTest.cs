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

    using NSubstitute;

    using Services.Keyboard.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class PasteCombinationDurationMediatorTest: UnitTestFor<IPasteCombinationDurationMediator>
    {
        static void RaiseHotkeyFired(IPasteHotkeyInterceptor fakePasteHotkeyInterceptor)
        {
            fakePasteHotkeyInterceptor.HotkeyFired += Raise.EventWith(
                fakePasteHotkeyInterceptor,
                new HotkeyFiredArgument(
                    Key.V,
                    true));
        }

        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            Assert.IsFalse(systemUnderTest.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfConsumerThreadIsRunning()
        {
            container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            Assert.IsTrue(systemUnderTest.IsConnected);
        }

        [TestMethod]
        public void CombinationIsHeldDownWhenCtrlVIsDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(systemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyCtrlIsDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(systemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyVIsDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsFalse(systemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenBothVAndCtrlIsReleased()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(systemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyVIsDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(systemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyCtrlIsDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsTrue(systemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenBothVAndCtrlAreDown()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(systemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationReleasedWhenBothVAndCtrlAreReleased()
        {
            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(systemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            systemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);

            systemUnderTest.Connect(Substitute.For<IHookableWindow>());
        }

        [TestMethod]
        public void WhenCtrlVIsDownMonitoringLoopRuns()
        {
            systemUnderTest.Connect(Substitute.For<IHookableWindow>());

            var fakePasteHotkeyInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            var fakeConsumerThreadLoop = container.Resolve<IConsumerThreadLoop>();
            fakeConsumerThreadLoop
                .Received()
                .Notify(
                    Arg.Any<Func<Task>>(),
                    Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
        public void WhenCombinationIsDownForLongEnoughThenEventFires()
        {
            var decisecondsPassed = 0;
            var holdCombinationDown = true;

            container.Resolve<IThreadDelay>()
                     .ExecuteAsync(
                         Arg.Do<int>(
                             x => decisecondsPassed += x / 100))
                     .Returns(Task.CompletedTask);

            container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(i => holdCombinationDown);
                             x.IsKeyDown(Key.V)
                              .Returns(i => holdCombinationDown);
                         });

            container.Resolve<IConsumerThreadLoop>()
                     .Notify(
                         Arg.Do<Func<Task>>(async x => await x()),
                         Arg.Any<CancellationToken>());
            
            systemUnderTest.PasteCombinationDurationPassed += (sender, e) => holdCombinationDown = false;
            systemUnderTest.Connect(Substitute.For<IHookableWindow>());

            var fakePasteHotkeyInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            Assert.AreEqual(systemUnderTest.DurationInDeciseconds, decisecondsPassed);
        }
    }
}