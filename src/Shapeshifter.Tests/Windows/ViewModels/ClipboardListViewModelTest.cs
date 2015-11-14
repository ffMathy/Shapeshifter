using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Binders.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;

namespace Shapeshifter.Tests.Windows.ViewModels
{
    [TestClass]
    public class ClipboardListViewModelTest : TestBase
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            var container =
                CreateContainer(
                    c => { c.RegisterFake<IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>>(); });

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
        public void SelectedElementChangesToSupportedActions()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Contents.Returns(new[] {fakeData});

            var supportedAction = Substitute.For<IAction>();
            supportedAction
                .CanPerformAsync(fakePackage)
                .Returns(Task.FromResult(true));

            var container =
                CreateContainer(
                    c => { c.RegisterFake<IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>>(); });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.SelectedElement = fakePackage;

            var fakeBinder = container.Resolve<IAsyncListDictionaryBinder<IClipboardDataControlPackage, IAction>>();
            fakeBinder.Received(1).LoadFromKey(fakePackage);
        }

        [TestMethod]
        public void ControlAddedSetsSelected()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c => { c.RegisterInstance(fakeUserInterfaceMediator); });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded += Raise.Event<EventHandler<ControlEventArgument>>(viewModel,
                new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.SelectedElement);
        }

        [TestMethod]
        public void ControlAddedInsertsElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c => { c.RegisterInstance(fakeUserInterfaceMediator); });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            fakeUserInterfaceMediator.ControlAdded += Raise.Event<EventHandler<ControlEventArgument>>(viewModel,
                new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }

        [TestMethod]
        public void ControlHighlightedMovesElement()
        {
            var fakeUserInterfaceMediator = Substitute.For<IClipboardUserInterfaceMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            var container = CreateContainer(c => { c.RegisterInstance(fakeUserInterfaceMediator); });

            var viewModel = container.Resolve<IClipboardListViewModel>();
            viewModel.Elements.Add(fakePackage);

            fakeUserInterfaceMediator.ControlHighlighted += Raise.Event<EventHandler<ControlEventArgument>>(viewModel,
                new ControlEventArgument(fakePackage));

            Assert.AreSame(fakePackage, viewModel.Elements.Single());
        }
    }
}