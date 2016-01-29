namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Windows.Input;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    [TestClass]
    public class PasteCombinationStateServiceTest: UnitTestFor<IPasteCombinationStateService>
    {
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
    }
}