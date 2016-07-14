namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Autofac;

    using Controls.Window.Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

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

            Container.Resolve<IClipboardListWindow>()
                     .SourceInitialized += Raise.Event();

            Container.Resolve<IClipboardUserInterfaceInteractionMediator>()
                .Received()
                .Connect();
        }
    }
}