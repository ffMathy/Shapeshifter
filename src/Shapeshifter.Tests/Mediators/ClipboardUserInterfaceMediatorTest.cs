namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Linq;

    using Autofac;

    using Controls.Clipboard.Factories.Interfaces;
    using Controls.Clipboard.Interfaces;
    using Controls.Window.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Messages.Interceptors.Interfaces;

    [TestClass]
    public class ClipboardUserInterfaceMediatorTest: TestBase
    {
        [TestMethod]
        public void CancelCancelsCombinationRegistrationOnDurationMediator()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Cancel();

            var fakeDurationMediator = container.Resolve<IPasteCombinationDurationMediator>();
            fakeDurationMediator.Received(1).CancelCombinationRegistration();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DisconnectWhileAlreadyDisconnectedThrowsException()
        {
            var container = CreateContainer();

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConnectWhileAlreadyConnectedThrowsException()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);
                });

            var fakeWindow = Substitute.For<IWindow>();

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(fakeWindow);
        }

        [TestMethod]
        public void IsConnectedIsFalseIfPasteCombinationDurationMonitorIsNotConnected()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(false);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfAllHooksAreConnected()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        public void ConnectAddsInitialPackage()
        {
            var fakeData = Substitute.For<IClipboardData>();
            var fakeControl = Substitute.For<IClipboardControl>();
            var fakePackage = Substitute.For<IClipboardDataControlPackage>();
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();
                    c.RegisterFake<IClipboardDataControlPackageFactory>()
                     .CreateFromCurrentClipboardData()
                     .Returns(fakePackage);
                    fakePackage.Data.Contents.Returns(new[] { fakeData });
                    fakePackage.Control.Returns(fakeControl);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var addedPackage = mediator.ClipboardElements.Single();
            var content = addedPackage.Data.Contents.Single();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void ConnectTriggersControlAddedEvent()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();
                    c.RegisterFake<IClipboardDataControlPackageFactory>();
                });
            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            object eventSender = null;
            ControlEventArgument eventArgument = null;
            mediator.ControlAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            mediator.Connect(null);

            var addedPackage = mediator.ClipboardElements.Single();
            Assert.IsNotNull(addedPackage);
            Assert.AreSame(mediator, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }


        [TestMethod]
        public void ConnectConnectsHotkeyHook()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();
                    c.RegisterFake<IClipboardDataControlPackageFactory>();
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var fakeHotkeyHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeHotkeyHookService.Received()
                                 .Connect(null);
        }

        [TestMethod]
        public void DisconnectDisconnectsKeyboardHook()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IPasteCombinationDurationMediator>()
                     .IsConnected
                     .Returns(true);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Disconnect();

            var fakeKeyboardHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeKeyboardHookService.Received()
                                   .Disconnect();
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToCreatePackage()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();

                    var fakePackage =
                        Substitute.For<IClipboardDataControlPackage>();
                    fakePackage.Data.Contents.Returns(
                        new[]
                        {
                            Substitute
                            .For
                            <IClipboardData>()
                        });

                    c.RegisterFake<IClipboardDataControlPackageFactory>()
                     .CreateFromCurrentClipboardData()
                     .Returns(fakePackage);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var numberOfPackagesBeforeDataCopied = mediator.ClipboardElements.Count();
            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            Assert.AreEqual(1, mediator.ClipboardElements.Count() - numberOfPackagesBeforeDataCopied);
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToDecoratePackageWithData()
        {
            var fakeData = Substitute.For<IClipboardData>();
            var fakeControl = Substitute.For<IClipboardControl>();

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();

                    var fakePackage =
                        Substitute.For<IClipboardDataControlPackage>();
                    fakePackage.Data.Contents.Returns(
                        new[]
                        {
                            fakeData
                        });
                    fakePackage.Control.Returns(fakeControl);

                    c.RegisterFake<IClipboardDataControlPackageFactory>(
                        
                        )
                     .CreateFromCurrentClipboardData()
                     .Returns(fakePackage);
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = mediator.ClipboardElements.Last();
            var content = addedPackage.Data.Contents.Last();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void DataCopiedTriggersEvent()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                    c.RegisterFake<IPasteCombinationDurationMediator>();
                    c.RegisterFake<IClipboardDataControlPackageFactory>(
                        
                        );
                });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            object eventSender = null;
            ControlEventArgument eventArgument = null;
            mediator.ControlAdded += (sender, e) => {
                eventSender = sender;
                eventArgument = e;
            };

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(
                    fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = mediator.ClipboardElements.Last();
            Assert.IsNotNull(addedPackage);

            Assert.AreSame(mediator, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }
    }
}