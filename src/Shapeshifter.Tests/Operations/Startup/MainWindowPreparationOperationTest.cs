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
            await SystemUnderTest.RunAsync();

            Container.Resolve<IClipboardListWindow>()
                .Received()
                .Show();
        }

        [TestMethod]
        public async Task WiresUserInterfaceMediatorUpWhenWindowIsLaunched()
        {
            await SystemUnderTest.RunAsync();

            var fakeWindow = Container.Resolve<IClipboardListWindow>();
            var fakeMediator = Container.Resolve<IClipboardUserInterfaceInteractionMediator>();

            fakeWindow.SourceInitialized += Raise.Event();
            fakeMediator.Received()
                        .Connect(fakeWindow);
        }
    }
}