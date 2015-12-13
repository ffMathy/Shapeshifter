namespace Shapeshifter.WindowsDesktop.Windows.ViewModels
{
    using System;
    using System.Linq;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Api;

    using Controls.Window.Binders.Interfaces;
    using Controls.Window.ViewModels.Interfaces;

    using Data.Interfaces;
    using Infrastructure.Events;
    using Mediators.Interfaces;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    using Data.Actions.Interfaces;

    [TestClass]
    public class ClipboardListViewModelTest: TestBase
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            var container =
                CreateContainer(
                    c =>
                    {
                        c.RegisterFake<IAsyncListDictionaryBinder
                                    <IClipboardDataControlPackage, IAction>>();
                    });

            var viewModel = container.Resolve<IClipboardListViewModel>();

            object eventSender = null;

            viewModel.PropertyChanged += (sender, e) =>
            {
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
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenSecondIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenFirstIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenThirdIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenSecondIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void SelectedActionChangesToTheSecondWhenFirstIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_LEFT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction2);
        }

        [TestMethod]
        public void SelectedActionChangesToTheThirdWhenSecondIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_RIGHT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction3);
        }

        [TestMethod]
        public void SelectedActionChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_LEFT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_DOWN,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction1);
        }

        [TestMethod]
        public void SelectedActionChangesToTheThirdWhenFirstIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_RIGHT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction3);
        }

        [TestMethod]
        public void SelectedActionChangesToTheSecondWhenThirdIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_LEFT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction2);
        }

        [TestMethod]
        public void SelectedActionChangesToTheFirstWhenSecondIsSelectedAndUpIsPressed()
        {
            var container = CreateContainer(
                c =>
                {
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
            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_RIGHT,
                    false));

            fakeKeyInterceptor.HotkeyFired += Raise.Event<EventHandler<HotkeyFiredArgument>>(
                new object
                    (),
                new HotkeyFiredArgument
                    (
                    KeyboardApi
                        .VK_KEY_UP,
                    false));

            Assert.AreSame(systemUnderTest.SelectedAction, fakeAction1);
        }

        [TestMethod]
        public void ControlAddedSetsSelected()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(
                c =>
                {
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
                c =>
                {
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

        [TestMethod]
        public void ControlHighlightedMovesElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(
                c =>
                {
                    c.RegisterInstance(fakeUserInterfaceMediator);
                });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.Elements.Add(fakePackage);

            fakeUserInterfaceMediator.ControlHighlighted +=
                Raise.Event<EventHandler<ControlEventArgument>>(
                    viewModel,
                    new ControlEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }
    }
}