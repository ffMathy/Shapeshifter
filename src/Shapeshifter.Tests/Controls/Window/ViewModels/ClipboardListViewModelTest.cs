namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System;
    using System.Linq;
    using System.Windows.Input;

    using Autofac;

    using Binders.Interfaces;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class ClipboardListViewModelTest: UnitTestFor<IClipboardListViewModel>
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            object eventSender = null;

            systemUnderTest.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(systemUnderTest.SelectedElement))
                {
                    eventSender = sender;
                }
            };
            systemUnderTest.SelectedElement = Substitute.For<IClipboardDataControlPackage>();

            Assert.AreSame(systemUnderTest, eventSender);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenFirstIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage1;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenSecondIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage2;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage3;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenFirstIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage1;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenThirdIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage3;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenSecondIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            systemUnderTest.Elements.Add(fakePackage1);
            systemUnderTest.Elements.Add(fakePackage2);
            systemUnderTest.Elements.Add(fakePackage3);

            systemUnderTest.SelectedElement = fakePackage2;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void SelectedActionChangesToTheSecondWhenFirstIsSelectedAndDownIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction1;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction2);
        }

        [TestMethod]
        public void SelectedActionChangesToTheThirdWhenSecondIsSelectedAndDownIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction2;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction3);
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInDataListAndLeftIsPressed()
        {
            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Left, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInActionListAndRightIsPressed()
        {
            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void WhenPasteIsRequestedSelectedActionIsInvoked()
        {
            var fakeAction = Substitute.For<IAction>();
            systemUnderTest.SelectedAction = fakeAction;

            var fakeElement = Substitute.For<IClipboardDataControlPackage>();
            systemUnderTest.SelectedElement = fakeElement;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.PastePerformed +=
                Raise.Event<EventHandler
                    <PastePerformedEventArgument>>(new object());

            fakeAction.Received()
                      .PerformAsync(Arg.Any<IClipboardDataPackage>());
        }

        [TestMethod]
        public void UserInterfaceShownIsBubbledUpFromDurationMediator()
        {
            var showEventCount = 0;
            
            systemUnderTest.UserInterfaceShown += (sender, e) => showEventCount++;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.UserInterfaceShown += Raise.Event<EventHandler<UserInterfaceShownEventArgument>>(new object());

            Assert.AreEqual(1, showEventCount);
        }

        [TestMethod]
        public void UserInterfaceHiddenIsBubbledUpFromDurationMediator()
        {
            var hideEventCount = 0;
            
            systemUnderTest.UserInterfaceHidden += (sender, e) => hideEventCount++;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.UserInterfaceHidden += Raise.Event<EventHandler<UserInterfaceHiddenEventArgument>>(new object());

            Assert.AreEqual(1, hideEventCount);
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInItemListAndLeftIsPressed()
        {
            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Left, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void CanAlternateBetweenListsWithoutCancellingMediator()
        {
            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Left, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.DidNotReceive()
                        .Cancel();
        }

        [TestMethod]
        public void SelectedActionChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction3;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Down, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction1);
        }

        [TestMethod]
        public void SelectedActionChangesToTheThirdWhenFirstIsSelectedAndUpIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction1;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction3);
        }

        [TestMethod]
        public void SelectedActionChangesToTheSecondWhenThirdIsSelectedAndUpIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction3;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction2);
        }

        [TestMethod]
        public void SelectedActionChangesToTheFirstWhenSecondIsSelectedAndUpIsPressed()
        {
            var fakeAction1 = Substitute.For<IAction>();
            var fakeAction2 = Substitute.For<IAction>();
            var fakeAction3 = Substitute.For<IAction>();

            systemUnderTest.Actions.Add(fakeAction1);
            systemUnderTest.Actions.Add(fakeAction2);
            systemUnderTest.Actions.Add(fakeAction3);

            systemUnderTest.SelectedAction = fakeAction2;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Up, false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction1);
        }

        [TestMethod]
        public void ControlAddedSetsSelected()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceInteractionMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            
            fakeUserInterfaceMediator.PackageAdded +=
                Raise.Event<EventHandler<PackageEventArgument>>(
                    fakePackage,
                    new PackageEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, systemUnderTest.SelectedElement);
        }

        [TestMethod]
        public void ControlAddedInsertsElement()
        {
            var fakeUserInterfaceMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            
            fakeUserInterfaceMediator.PackageAdded +=
                Raise.Event<EventHandler<PackageEventArgument>>(
                    systemUnderTest,
                    new PackageEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, systemUnderTest.Elements.Single());
        }
    }
}