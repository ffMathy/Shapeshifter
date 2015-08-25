using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.Core.Actions;
using System.Collections.Generic;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.ComponentModel;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Tests.Windows.ViewModels
{
    [TestClass]
    public class ClipboardListViewModelTest : TestBase
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            var container = CreateContainer();

            var viewModel = container.Resolve<IClipboardListViewModel>();

            object eventSender = null;
            PropertyChangedEventArgs eventArguments = null;

            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(viewModel.SelectedElement))
                {
                    eventSender = sender;
                    eventArguments = e;
                }
            };
            viewModel.SelectedElement = Substitute.For<IClipboardDataControlPackage>();

            Assert.AreSame(viewModel, eventSender);
        }

        [TestMethod]
        public void SelectedActionChangedTriggersChangedEvent()
        {
            var container = CreateContainer();

            var viewModel = container.Resolve<IClipboardListViewModel>();

            object eventSender = null;
            PropertyChangedEventArgs eventArguments = null;

            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof(viewModel.SelectedAction))
                {
                    eventSender = sender;
                    eventArguments = e;
                }
            };
            viewModel.SelectedAction = Substitute.For<IAction>();

            Assert.AreSame(viewModel, eventSender);
        }

        [TestMethod]
        public void SelectedElementChangesToSupportedActions()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Contents.Returns(new[] { fakeData });

            var supportedAction = Substitute.For<IAction>();
            supportedAction.CanPerformAsync(fakePackage).Returns(Task.FromResult(true));

            var container = CreateContainer(c =>
            {

                c.RegisterInstance<IEnumerable<IAction>>(new[] { supportedAction });
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.SelectedElement = fakePackage;

            Assert.IsTrue(viewModel.Actions.Contains(supportedAction));
        }

        [TestMethod]
        public void SelectedElementChangesActionsWithoutUnsupportedActions()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Contents.Returns(new[] { fakeData });

            var unsupportedAction = Substitute.For<IAction>();
            unsupportedAction.CanPerformAsync(fakePackage).Returns(Task.FromResult(false));

            var container = CreateContainer(c =>
            {
                c.RegisterInstance<IEnumerable<IAction>>(new[] { unsupportedAction });
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.SelectedElement = fakePackage;

            Assert.IsFalse(viewModel.Actions.Contains(unsupportedAction));
        }

        [TestMethod]
        public void ControlAddedSetsSelected()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c =>
            {
                c.RegisterInstance(fakeUserInterfaceMediator);
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded += Raise.Event<EventHandler<ControlEventArgument>>(viewModel, new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.SelectedElement);
        }

        [TestMethod]
        public void ControlAddedInsertsElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c =>
            {
                c.RegisterInstance(fakeUserInterfaceMediator);
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded += Raise.Event<EventHandler<ControlEventArgument>>(viewModel, new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }

        [TestMethod]
        public void ControlRemovedRemovesElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c =>
            {
                c.RegisterInstance(fakeUserInterfaceMediator);
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.Elements.Add(fakePackage);

            fakeUserInterfaceMediator.ControlRemoved += Raise.Event<EventHandler<ControlEventArgument>>(viewModel, new ControlEventArgument(fakePackage));

            Assert.IsFalse(viewModel.Elements.Any());
        }

        [TestMethod]
        public void ControlHighlightedMovesElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c =>
            {
                c.RegisterInstance(fakeUserInterfaceMediator);
            });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.Elements.Add(fakePackage);

            fakeUserInterfaceMediator.ControlHighlighted += Raise.Event<EventHandler<ControlEventArgument>>(viewModel, new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }
    }
}
