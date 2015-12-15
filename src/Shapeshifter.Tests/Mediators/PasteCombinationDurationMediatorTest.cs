namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Windows.Input;

    using Autofac;

    using Controls.Window.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Keyboard.Interfaces;

    [TestClass]
    public class PasteCombinationDurationMediatorTest: TestBase
    {

        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            var container = CreateContainer(
                c => {
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
                c => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
                c => {
                    c.RegisterFake<IKeyboardManager>()
                     .WithFakeSettings(
                         x => {
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
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Connect(Substitute.For<IWindow>());
        }
    }
}