namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;

    using Autofac;

    using Controls.Window.Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Operations.Startup;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;

    [TestClass]
    public class ApplicationEntryPointTest: UnitTestFor<ApplicationEntrypoint>
    {
        [TestMethod]
        public async Task CanStartWithNoTerminatingArgumentProcessorShowsMainWindow()
        {
            await systemUnderTest.Start();

            container.Resolve<IClipboardListWindow>()
                .Received()
                .Show();
        }

        [TestMethod]
        public async Task WiresUserInterfaceMediatorUpWhenWindowIsLaunched()
        {
            await systemUnderTest.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            var fakeMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            fakeWindow.SourceInitialized += Raise.Event();
            fakeMediator.Received()
                        .Connect(fakeWindow);
        }

        [TestMethod]
        public async Task CanStartWithTerminatingArgumentProcessorShowsMainWindow()
        {
            container
                .Resolve<ISingleArgumentProcessor>()
                .With(
                    x => {
                        x.Terminates.Returns(true);
                        x.CanProcess(Arg.Any<string[]>())
                        .Returns(true);
                    });
            
            await systemUnderTest.Start();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            fakeWindow.DidNotReceive()
                      .Show();
        }
    }
}