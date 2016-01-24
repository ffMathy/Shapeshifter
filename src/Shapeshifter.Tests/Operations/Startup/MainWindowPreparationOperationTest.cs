namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using NSubstitute;

    using WindowsDesktop;

    using Controls.Window.Interfaces;

    using Mediators.Interfaces;

    using Operations.Startup.Interfaces;

    [TestClass]
    public class MainWindowPreparationOperationTest: UnitTestFor<IMainWindowPreparationOperation>
    {
        [TestMethod]
        public async Task PreparingWindowShowsIt()
        {
            await systemUnderTest.RunAsync();

            container.Resolve<IClipboardListWindow>()
                .Received()
                .Show();
        }

        [TestMethod]
        public async Task WiresUserInterfaceMediatorUpWhenWindowIsLaunched()
        {
            await systemUnderTest.RunAsync();

            var fakeWindow = container.Resolve<IClipboardListWindow>();
            var fakeMediator = container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            fakeWindow.SourceInitialized += Raise.Event();
            fakeMediator.Received()
                        .Connect(fakeWindow);
        }
    }
}