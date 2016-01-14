namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;

    using Autofac;

    using Controls.Window.Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;

    [TestClass]
    public class MainTest: TestBase
    {
        [TestMethod]
        public async Task CanStartWithNoTerminatingArgumentProcessorShowsMainWindow()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardListWindow>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IProcessManager>();
                });

            var main = container.Resolve<Main>();
            await main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            fakeWindow.Received()
                      .Show();
        }

        [TestMethod]
        public async Task WiresUserInterfaceMediatorUpWhenWindowIsLaunched()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardListWindow>();
                    c.RegisterFake<IClipboardUserInterfaceMediator>();
                    c.RegisterFake<IProcessManager>();
                });

            var main = container.Resolve<Main>();
            await main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            var fakeMediator = container.Resolve<IClipboardUserInterfaceMediator>();

            fakeWindow.SourceInitialized += Raise.Event();
            fakeMediator.Received()
                        .Connect(fakeWindow);
        }

        [TestMethod]
        public async Task CanStartWithTerminatingArgumentProcessorShowsMainWindow()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<ISingleArgumentProcessor>()
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
            await main.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            fakeWindow.DidNotReceive()
                      .Show();
        }
    }
}