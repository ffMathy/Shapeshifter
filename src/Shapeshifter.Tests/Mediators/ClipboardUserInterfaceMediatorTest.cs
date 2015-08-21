using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Shapeshifter.Tests.Mediators
{
    [TestClass]
    public class ClipboardUserInterfaceMediatorTest : TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfClipboardHookIsNotConnected()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(false);

                c.RegisterFake<IPasteHotkeyInterceptor>()
                    .IsConnected
                    .Returns(true);
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsFalseIfKeyboardHookIsNotConnected()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(true);

                c.RegisterFake<IPasteHotkeyInterceptor>()
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
                c.RegisterFake<IClipboardHookService>()
                    .IsConnected
                    .Returns(true);

                c.RegisterFake<IPasteHotkeyInterceptor>()
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
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            var fakeHotkeyHookService = container.Resolve<IPasteHotkeyInterceptor>();
            fakeHotkeyHookService.Received().Connect();
        }

        [TestMethod]
        public void ConnectConnectsClipboardHook()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.Received().Connect();
        }

        [TestMethod]
        public void DisconnectDisconnectsKeyboardHook()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Disconnect();

            var fakeKeyboardHookService = container.Resolve<IPasteHotkeyInterceptor>();
            fakeKeyboardHookService.Received().Disconnect();
        }

        [TestMethod]
        public void DisconnectDisconnectsClipboardHook()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Disconnect();

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.Received().Disconnect();
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToCreatePackage()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();

                var fakeFactory = Substitute.For<IClipboardDataControlFactory>();
                c.RegisterInstance<IEnumerable<IClipboardDataControlFactory>>(new[] { fakeFactory });
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            var fakeDataObject = Substitute.For<IDataObject>();

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.DataCopied += Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService, new DataCopiedEventArgument(fakeDataObject));

            Assert.AreEqual(1, mediator.ClipboardElements.Count());
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToDecoratePackageWithData()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();

                var fakeFactory = Substitute.For<IClipboardDataControlFactory>();
                fakeFactory
                    .CanBuildData(Arg.Any<string>())
                    .Returns(true);

                fakeFactory
                    .BuildData(Arg.Any<string>(), Arg.Any<object>())
                    .Returns(fakeData);
                c.RegisterInstance<IEnumerable<IClipboardDataControlFactory>>(new[] { fakeFactory });
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            var fakeDataObject = Substitute.For<IDataObject>();
            fakeDataObject
                .GetFormats(true)
                .Returns(new[] { "foobar" });

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.DataCopied += Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService, new DataCopiedEventArgument(fakeDataObject));

            var addedPackage = mediator.ClipboardElements.Single();
            var content = addedPackage.Contents.Single();
            Assert.AreSame(fakeData, content);
        }

        [TestMethod]
        public void DataCopiedCausesMediatorToDecoratePackageWithControl()
        {
            var fakeControl = Substitute.For<IClipboardControl>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();

                var fakeFactory = Substitute.For<IClipboardDataControlFactory>();
                fakeFactory
                    .CanBuildData(Arg.Any<string>())
                    .Returns(true);

                fakeFactory
                    .CanBuildControl(Arg.Any<IClipboardData>())
                    .Returns(true);

                fakeFactory
                    .BuildControl(Arg.Any<IClipboardData>())
                    .Returns(fakeControl);
                c.RegisterInstance<IEnumerable<IClipboardDataControlFactory>>(new[] { fakeFactory });
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            var fakeDataObject = Substitute.For<IDataObject>();
            fakeDataObject
                .GetFormats(true)
                .Returns(new[] { "foobar" });

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.DataCopied += Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService, new DataCopiedEventArgument(fakeDataObject));

            var addedPackage = mediator.ClipboardElements.Single();
            Assert.AreSame(fakeControl, addedPackage.Control);
        }

        [TestMethod]
        public void DataCopiedTriggersEvent()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardHookService>();
                c.RegisterFake<IPasteHotkeyInterceptor>();
            });

            var mediator = container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();

            object eventSender = null;
            ControlEventArgument eventArgument = null;
            mediator.ControlAdded += (sender, e) =>
            {
                eventSender = sender;
                eventArgument = e;
            };

            var fakeDataObject = Substitute.For<IDataObject>();

            var fakeClipboardHookService = container.Resolve<IClipboardHookService>();
            fakeClipboardHookService.DataCopied += Raise.Event<EventHandler<DataCopiedEventArgument>>(fakeClipboardHookService, new DataCopiedEventArgument(fakeDataObject));

            var addedPackage = mediator.ClipboardElements.Single();
            Assert.IsNotNull(addedPackage);

            Assert.AreSame(mediator, eventSender);
            Assert.AreSame(addedPackage, eventArgument.Package);
        }
    }
}
