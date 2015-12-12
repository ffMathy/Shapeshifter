namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System.Collections.Generic;

    using Windows.Interfaces;

    using Autofac;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;
    using Services.Arguments.Interfaces;

    [TestClass]
    public class MainTest: TestBase
    {
        [TestMethod]
        public void CanStartWithNoTerminatingArgumentProcessorShowsMainWindow()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardListWindow>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IProcessManager>();
                });

            var main = container.Resolve<Main>();
            main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            fakeWindow.Received().Show();
        }

        [TestMethod]
        public void WiresUserInterfaceMediatorUpWhenWindowIsLaunched()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardListWindow>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IProcessManager>();
                });

            var main = container.Resolve<Main>();
            main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            var fakeMediator = container.Resolve<IClipboardUserInterfaceMediator>();

            fakeWindow.SourceInitialized += Raise.Event();
            fakeMediator.Received()
                        .Connect(fakeWindow);
        }

        [TestMethod]
        public void CanStartWithTerminatingArgumentProcessorShowsMainWindow()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IArgumentProcessor>()
                     .WithFakeSettings(
                         x => {
                             x.Terminates.Returns(true);
                             x.CanProcess(Arg.Any<string[]>())
                              .Returns(true);
                         });

                    c.RegisterFake<IClipboardListWindow>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IProcessManager>();
                });

            var main = container.Resolve<Main>();
            main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            fakeWindow.DidNotReceive().Show();
        }
    }
}