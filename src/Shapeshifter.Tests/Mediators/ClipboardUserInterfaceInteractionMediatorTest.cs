namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Linq;
    using System.Windows.Input;

    using Autofac;

    using Controls.Clipboard.Factories.Interfaces;
    using Controls.Clipboard.Interfaces;
    using Controls.Window.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interceptors.Interfaces;

    [TestClass]
    public class ClipboardUserInterfaceInteractionMediatorTest: UnitTestFor<IClipboardUserInterfaceInteractionMediator>
    {
        [TestMethod]
        public void CancelCancelsCombinationRegistrationOnDurationMediator()
        {
            container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
            
            systemUnderTest.Cancel();

            var fakeDurationMediator = container.Resolve<IPasteCombinationDurationMediator>();
            fakeDurationMediator.Received(1)
                                .CancelCombinationRegistration();
        }

        [TestMethod]
        public void MediatorIsCancelledWhenInActionListAndRightIsPressed()
        {
            var called = false;
            systemUnderTest.UserInterfaceHidden += (sender, argument) =>
                                                   called = true;

            var fakeKeyInterceptor = container.Resolve<IKeyInterceptor>();
            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            fakeKeyInterceptor.HotkeyFired +=
                Raise.Event<EventHandler<HotkeyFiredArgument>>(
                    new object(),
                    new HotkeyFiredArgument(Key.Right, false));

            Assert.IsTrue(called);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectWhileAlreadyDisconnectedThrowsException()
        {
            systemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectWhileAlreadyConnectedThrowsException()
        {
            container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);

            var fakeWindow = Substitute.For<IHookableWindow>();
            systemUnderTest.Connect(fakeWindow);
        }

        [TestMethod]
        public void IsConnectedIsFalseIfPasteCombinationDurationMonitorIsNotConnected()
        {
            container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
            
            Assert.IsFalse(systemUnderTest.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfAllHooksAreConnected()
        {
            container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);
            
            Assert.IsTrue(systemUnderTest.IsConnected);
        }

        [TestMethod]
        public void ConnectAddsInitialPackage()
        {
            var fakeData = Substitute.For<IClipboardData>();
            var fakeControl = Substitute.For<IClipboardControl>();
            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Data.Contents.Returns(
                new[]
                {
                            fakeData
                });
            fakePackage.Control.Returns(fakeControl);

            container.Resolve<IClipboardDataControlPackageFactory>()
                     .CreateFromCurrentClipboardData()
                     .Returns(fakePackage);

            var mediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();
            mediator.Connect(null);

            var addedPackage = mediator.ClipboardElements.Single();
            var content = addedPackage.Data.Contents.Single();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void ConnectTriggersControlAddedEvent()
        {
            object eventSender = null;
            PackageEventArgument eventArgument = null;
            systemUnderTest.PackageAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            systemUnderTest.Connect(null);

            var addedPackage = systemUnderTest.ClipboardElements.Single();
            Assert.IsNotNull(addedPackage);
            Assert.AreSame(systemUnderTest, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }

        [TestMethod]
        public void ConnectConnectsHotkeyHook()
        {
            systemUnderTest.Connect(null);

            var fakeHotkeyHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeHotkeyHookService.Received()
                                 .Connect(null);
        }

        [TestMethod]
        public void DisconnectDisconnectsKeyboardHook()
        {
            container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);

            systemUnderTest.Disconnect();

            var fakeKeyboardHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeKeyboardHookService.Received()
                                   .Disconnect();
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToCreatePackage()
        {

            var fakePackage =
                Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Data.Contents.Returns(
                new[]
                {
                            Substitute
                            .For
                            <IClipboardData>()
                });

            container.Resolve<IClipboardDataControlPackageFactory>()
             .CreateFromCurrentClipboardData()
             .Returns(fakePackage);
            
            systemUnderTest.Connect(null);

            var numberOfPackagesBeforeDataCopied = systemUnderTest.ClipboardElements.Count();
            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            Assert.AreEqual(1, systemUnderTest.ClipboardElements.Count() - numberOfPackagesBeforeDataCopied);
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToDecoratePackageWithData()
        {
            var fakeData = Substitute.For<IClipboardData>();
            var fakeControl = Substitute.For<IClipboardControl>();

            var fakePackage =
                Substitute.For<IClipboardDataControlPackage>();
            fakePackage.Data.Contents.Returns(
                new[]
                {
                            fakeData
                });
            fakePackage.Control.Returns(fakeControl);

            container.Resolve<IClipboardDataControlPackageFactory>()
             .CreateFromCurrentClipboardData()
             .Returns(fakePackage);
            
            systemUnderTest.Connect(null);

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = systemUnderTest.ClipboardElements.Last();
            var content = addedPackage.Data.Contents.Last();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void DataCopiedTriggersEvent()
        {
            systemUnderTest.Connect(null);

            object eventSender = null;
            PackageEventArgument eventArgument = null;
            systemUnderTest.PackageAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = systemUnderTest.ClipboardElements.Last();
            Assert.IsNotNull(addedPackage);

            Assert.AreSame(systemUnderTest, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }
    }
}