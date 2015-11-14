using System;
using System.Linq;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces;

namespace Shapeshifter.Tests.Mediators
{
    [TestClass]
    public class ClipboardUserInterfaceMediatorTest : TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfPasteCombinationDurationMonitorIsNotConnected()
        {
            var container = CreateContainer(c =>
            {
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
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IPasteCombinationDurationMediator>()
                    .IsConnected
                    .Returns(true);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        public void ConnectConnectsHotkeyHook()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardCopyInterceptor>();
                c.RegisterFake<IPasteCombinationDurationMediator>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var fakeHotkeyHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeHotkeyHookService.Received().Connect(null);
        }

        [TestMethod]
        public void DisconnectDisconnectsKeyboardHook()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IPasteCombinationDurationMediator>()
                    .IsConnected
                    .Returns(true);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Disconnect();

            var fakeKeyboardHookService = container.Resolve<IPasteCombinationDurationMediator>();
            fakeKeyboardHookService.Received().Disconnect();
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToCreatePackage()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardCopyInterceptor>();
                c.RegisterFake<IPasteCombinationDurationMediator>();

                var fakePackage = Substitute.For<IClipboardDataControlPackage>();
                fakePackage.Contents.Returns(new[] {Substitute.For<IClipboardData>()});

                c.RegisterFake<IClipboardDataControlPackageFactory>()
                    .Create()
                    .Returns(fakePackage);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService,
                    new DataCopiedEventArgument());

            Assert.AreEqual(1, mediator.ClipboardElements.Count());
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToDecoratePackageWithData()
        {
            var fakeData = Substitute.For<IClipboardData>();
            var fakeControl = Substitute.For<IClipboardControl>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardCopyInterceptor>();
                c.RegisterFake<IPasteCombinationDurationMediator>();

                var fakePackage = Substitute.For<IClipboardDataControlPackage>();
                fakePackage.Contents.Returns(new[] {fakeData});
                fakePackage.Control.Returns(fakeControl);

                c.RegisterFake<IClipboardDataControlPackageFactory>()
                    .Create()
                    .Returns(fakePackage);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = mediator.ClipboardElements.Single();
            var content = addedPackage.Contents.Single();
            Assert.AreSame(fakeData, content);
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void DataCopiedTriggersEvent()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardCopyInterceptor>();
                c.RegisterFake<IPasteCombinationDurationMediator>();
                c.RegisterFake<IClipboardDataControlPackageFactory>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect(null);

            object eventSender = null;
            ControlEventArgument eventArgument = null;
            mediator.ControlAdded += (sender, e) =>
            {
                eventSender = sender;
                eventArgument = e;
            };

            var fakeClipboardHookService = container.Resolve<IClipboardCopyInterceptor>();
            fakeClipboardHookService.DataCopied +=
                Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService,
                    new DataCopiedEventArgument());

            var addedPackage = mediator.ClipboardElements.Single();
            Assert.IsNotNull(addedPackage);

            Assert.AreSame(mediator, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }
    }
}