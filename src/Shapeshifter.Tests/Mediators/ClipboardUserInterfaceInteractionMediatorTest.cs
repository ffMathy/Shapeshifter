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
            Container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
            
            SystemUnderTest.Cancel();

            var fakeDurationMediator = Container.Resolve<IPasteCombinationDurationMediator>();
            fakeDurationMediator.Received(1)
                                .CancelCombinationRegistration();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectWhileAlreadyDisconnectedThrowsException()
        {
            SystemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectWhileAlreadyConnectedThrowsException()
        {
            Container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);

            var fakeWindow = Substitute.For<IHookableWindow>();
            SystemUnderTest.Connect(fakeWindow);
        }

        [TestMethod]
        public void IsConnectedIsFalseIfPasteCombinationDurationMonitorIsNotConnected()
        {
            Container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
            
            Assert.IsFalse(SystemUnderTest.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfAllHooksAreConnected()
        {
            Container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);
            
            Assert.IsTrue(SystemUnderTest.IsConnected);
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

            Container.Resolve<IClipboardDataControlPackageFactory>()
                     .CreateFromCurrentClipboardData()
                     .Returns(fakePackage);

            var mediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();
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
            SystemUnderTest.PackageAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            SystemUnderTest.Connect(null);

            var addedPackage = SystemUnderTest.ClipboardElements.Single();
            Assert.IsNotNull(addedPackage);
            Assert.AreSame(SystemUnderTest, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }

        [TestMethod]
        public void ConnectConnectsHotkeyHook()
        {
            SystemUnderTest.Connect(null);

            var fakeHotkeyHookService = Container.Resolve<IPasteCombinationDurationMediator>();
            fakeHotkeyHookService.Received()
                                 .Connect(null);
        }

        [TestMethod]
        public void DisconnectDisconnectsKeyboardHook()
        {
            Container.Resolve<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);

            SystemUnderTest.Disconnect();

            var fakeKeyboardHookService = Container.Resolve<IPasteCombinationDurationMediator>();
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

            Container.Resolve<IClipboardDataControlPackageFactory>()
             .CreateFromCurrentClipboardData()
             .Returns(fakePackage);
            
            SystemUnderTest.Connect(null);

            var numberOfPackagesBeforeDataCopied = SystemUnderTest.ClipboardElements.Count();
            var fakeClipboardHookService = Container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            Assert.AreEqual(1, SystemUnderTest.ClipboardElements.Count() - numberOfPackagesBeforeDataCopied);
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

            Container.Resolve<IClipboardDataControlPackageFactory>()
             .CreateFromCurrentClipboardData()
             .Returns(fakePackage);
            
            SystemUnderTest.Connect(null);

            var fakeClipboardHookService = Container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = SystemUnderTest.ClipboardElements.Last();
            var content = addedPackage.Data.Contents.Last();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void DataCopiedTriggersEvent()
        {
            SystemUnderTest.Connect(null);

            object eventSender = null;
            PackageEventArgument eventArgument = null;
            SystemUnderTest.PackageAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            var fakeClipboardHookService = Container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = SystemUnderTest.ClipboardElements.Last();
            Assert.IsNotNull(addedPackage);

            Assert.AreSame(SystemUnderTest, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }
    }
}