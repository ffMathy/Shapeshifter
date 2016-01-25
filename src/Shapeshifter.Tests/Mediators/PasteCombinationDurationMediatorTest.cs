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
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            Assert.IsFalse(SystemUnderTest.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfConsumerThreadIsRunning()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
            
            Assert.IsTrue(SystemUnderTest.IsConnected);
        }

        [TestMethod]
        public void CombinationIsHeldDownWhenCtrlVIsDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(SystemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyCtrlIsDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(SystemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenOnlyVIsDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsFalse(SystemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void CombinationIsReleasedWhenBothVAndCtrlIsReleased()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(SystemUnderTest.IsCombinationFullyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyVIsDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(SystemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenOnlyCtrlIsDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsTrue(SystemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationIsHeldDownWhenBothVAndCtrlAreDown()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(true);
                             x.IsKeyDown(Key.V)
                              .Returns(true);
                         });
            
            Assert.IsTrue(SystemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        public void PartialCombinationReleasedWhenBothVAndCtrlAreReleased()
        {
            Container.Resolve<IKeyboardManager>()
                     .With(
                         x => {
                             x.IsKeyDown(Key.LeftCtrl)
                              .Returns(false);
                             x.IsKeyDown(Key.V)
                              .Returns(false);
                         });
            
            Assert.IsFalse(SystemUnderTest.IsCombinationPartiallyHeldDown);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            SystemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);

            SystemUnderTest.Connect(Substitute.For<IHookableWindow>());
        }

        [TestMethod]
        public void WhenCtrlVIsDownMonitoringLoopRuns()
        {
            SystemUnderTest.Connect(Substitute.For<IHookableWindow>());

            var fakePasteHotkeyInterceptor = Container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            var fakeConsumerThreadLoop = Container.Resolve<IConsumerThreadLoop>();
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
            //var decisecondsPassed = 0;
            //var holdCombinationDown = true;

            //container.Resolve<IThreadDelay>()
            //         .ExecuteAsync(
            //             Arg.Do<int>(
            //                 x => decisecondsPassed += x / 100))
            //         .Returns(Task.CompletedTask);

            //container.Resolve<IKeyboardManager>()
            //         .With(
            //             x => {
            //                 x.IsKeyDown(Key.LeftCtrl)
            //                  .Returns(i => holdCombinationDown);
            //                 x.IsKeyDown(Key.V)
            //                  .Returns(i => holdCombinationDown);
            //             });

            //container.Resolve<IConsumerThreadLoop>()
            //         .Notify(
            //             Arg.Do<Func<Task>>(async x => await x()),
            //             Arg.Any<CancellationToken>());
            
            //systemUnderTest.PasteCombinationDurationPassed += (sender, e) => holdCombinationDown = false;
            //systemUnderTest.Connect(Substitute.For<IHookableWindow>());

            //var fakePasteHotkeyInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            //RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            //Assert.AreEqual(systemUnderTest.DurationInDeciseconds, decisecondsPassed);
        }
    }
}