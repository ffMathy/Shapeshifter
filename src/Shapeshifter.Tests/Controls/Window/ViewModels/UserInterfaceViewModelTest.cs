namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using System;
    using System.Linq;
	using System.Threading.Tasks;

	using Autofac;

	using Data.Actions.Interfaces;
	using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class UserInterfaceViewModelTest : UnitTestFor<IUserInterfaceViewModel>
    {
        [TestMethod]
        public void SelectedElementChangedTriggersChangedEvent()
        {
            object eventSender = null;

            SystemUnderTest.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(SystemUnderTest.SelectedElement))
                {
                    eventSender = sender;
                }
            };
            SystemUnderTest.SelectedElement = Substitute.For<IClipboardDataControlPackage>();

            Assert.AreSame(SystemUnderTest, eventSender);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenFirstIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage1;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedNextItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenSecondIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage2;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedNextItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenThirdIsSelectedAndDownIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage3;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedNextItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void SelectedElementChangesToTheThirdWhenFirstIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage1;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedPreviousItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage3);
        }

        [TestMethod]
        public void SelectedElementChangesToTheSecondWhenThirdIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage3;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedPreviousItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage2);
        }

        [TestMethod]
        public void SelectedElementChangesToTheFirstWhenSecondIsSelectedAndUpIsPressed()
        {
            var fakePackage1 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage2 = Substitute.For<IClipboardDataControlPackage>();
            var fakePackage3 = Substitute.For<IClipboardDataControlPackage>();

            SystemUnderTest.Elements.Add(fakePackage1);
            SystemUnderTest.Elements.Add(fakePackage2);
            SystemUnderTest.Elements.Add(fakePackage3);

            SystemUnderTest.SelectedElement = fakePackage2;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.SelectedPreviousItem += Raise.Event();

            Assert.AreSame(SystemUnderTest.SelectedElement, fakePackage1);
        }

        [TestMethod]
        public void UserInterfaceHiddenIsBubbledUpFromDurationMediator()
        {
            var hideEventCount = 0;

            SystemUnderTest.UserInterfaceHidden += (sender, e) => hideEventCount++;

            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            fakeMediator.UserInterfaceHidden += Raise.Event<EventHandler<UserInterfaceHiddenEventArgument>>(new object());

            Assert.AreEqual(1, hideEventCount);
        }

        [TestMethod]
        public void ControlAddedSetsSelected()
        {
            var fakeUserInterfaceMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            fakeUserInterfaceMediator.PackageAdded +=
                Raise.Event<EventHandler<PackageEventArgument>>(
                    fakePackage,
                    new PackageEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, SystemUnderTest.SelectedElement);
        }

        [TestMethod]
        public void ControlAddedInsertsElement()
        {
            var fakeUserInterfaceMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            var fakePackage = Substitute.For<IClipboardDataControlPackage>();

            fakeUserInterfaceMediator.PackageAdded +=
                Raise.Event<EventHandler<PackageEventArgument>>(
                    SystemUnderTest,
                    new PackageEventArgument(
                        fakePackage));

            Assert.AreSame(fakePackage, SystemUnderTest.Elements.Single());
        }
    }
}