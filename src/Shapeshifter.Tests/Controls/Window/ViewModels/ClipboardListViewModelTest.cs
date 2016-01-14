namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System;
    using System.Linq;
    using System.Windows.Input;

    using Autofac;

    using Binders.Interfaces;
    using Interfaces;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Events;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    using NSubstitute;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class ClipboardListViewModelTest: TestBase
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            var container =
                CreateContainer(
                    c => {
                        c.RegisterFake<IAsyncListDictionaryBinder
                            <IClipboardDataControlPackage, IAction>>();
                    });

            var viewModel = container.Resolve<IClipboardListViewModel>();

            object eventSender = null;

            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(viewModel.SelectedElement))
                {
                    eventSender = sender;
                }
            };
            viewModel.SelectedElement = Substitute.For<IClipboardDataControlPackage>();

            Assert.AreSame(viewModel, eventSender);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenFirstIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IAsyncListDictionaryBinder
                        <IClipboardDataControlPackage, IAction>>();
                });
            
            container.Resolve<IClipboardListViewModel>();

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Left, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInActionListAndRightIsPressed()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IAsyncListDictionaryBinder
                        <IClipboardDataControlPackage, IAction>>();
                });

            container.Resolve<IClipboardListViewModel>();

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
                .Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void WhenPasteIsRequestedSelectedActionIsInvoked()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c
                        .RegisterFake<IAsyncListDictionaryBinder
                            <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

            var fakeAction = Substitute.For<IAction>();
            systemUnderTest.SelectedAction = fakeAction;

            var fakeElement = Substitute.For<IClipboardDataControlPackage>();
            systemUnderTest.SelectedElement = fakeElement;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.PastePerformed +=
                Raise.Event<EventHandler
                    <PastePerformedEventArgument>>(new object());

            fakeAction.Received()
                      .PerformAsync(Arg.Any<IClipboardDataPackage>());
        }

        [TestMethod]
        public void UserInterfaceShownIsBubbledUpFromDurationMediator()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c
                        .RegisterFake<IAsyncListDictionaryBinder
                            <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var showEventCount = 0;

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();
            systemUnderTest.UserInterfaceShown += (sender, e) => showEventCount++;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.UserInterfaceShown += Raise.Event<EventHandler<UserInterfaceShownEventArgument>>(new object());

            Assert.AreEqual(1, showEventCount);
        }

        [TestMethod]
        public void UserInterfaceHiddenIsBubbledUpFromDurationMediator()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c
                        .RegisterFake<IAsyncListDictionaryBinder
                            <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var hideEventCount = 0;

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();
            systemUnderTest.UserInterfaceHidden += (sender, e) => hideEventCount++;

            var fakeMediator = container.Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.UserInterfaceHidden += Raise.Event<EventHandler<UserInterfaceHiddenEventArgument>>(new object());

            Assert.AreEqual(1, hideEventCount);
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInItemListAndLeftIsPressed()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IAsyncListDictionaryBinder
                        <IClipboardDataControlPackage, IAction>>();
                });

            container.Resolve<IClipboardListViewModel>();

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Left, false));

            var fakeMediator = container
                .Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.Received()
                        .Cancel();
        }

        [TestMethod]
        public void CanAlternateBetweenListsWithoutCancellingMediator()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IAsyncListDictionaryBinder
                        <IClipboardDataControlPackage, IAction>>();
                });

            container.Resolve<IClipboardListViewModel>();

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
                .Resolve<IClipboardUserInterfaceMediator>();
            fakeMediator.DidNotReceive()
                        .Cancel();
        }

        [TestMethod]
        public void SelectedActionChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IKeyInterceptor>();
                    c
                        .RegisterFake
                        <
                            IAsyncListDictionaryBinder
                                <IClipboardDataControlPackage, IAction>>
                        ();
                });

            var systemUnderTest = container.Resolve<IClipboardListViewModel>();

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
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(
                c => {
                    c.RegisterInstance(fakeUserInterfaceMediator);
                });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded +=
                Raise.Event<EventHandler<ControlEventArgument>>(
                    viewModel,
                    new ControlEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, viewModel.SelectedElement);
        }

        [TestMethod]
        public void ControlAddedInsertsElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(
                c => {
                    c.RegisterInstance(fakeUserInterfaceMediator);
                });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded +=
                Raise.Event<EventHandler<ControlEventArgument>>(
                    viewModel,
                    new ControlEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }
    }
}