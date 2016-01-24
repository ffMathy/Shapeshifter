namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;

    using Autofac;

    using Controls.Window.Interfaces;

    using Mediators.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Operations.Startup;
    using Operations.Startup.Interfaces;

    using Services.Arguments.Interfaces;

    [TestClass]
    public class ApplicationEntryPointTest: UnitTestFor<ApplicationEntrypoint>
    {
        [TestMethod]
        public async Task CanStartWithTerminatingArgumentProcessorShowsMainWindow()
        {
            container
                .Resolve<IStartupPreparationOperation>()
                .ShouldTerminate.Returns(true);
            
            await systemUnderTest.Start();

            var fakeWindow = container.Resolve<IMainWindowPreparationOperation>();
            fakeWindow.DidNotReceive()
                      .RunAsync()
                      .IgnoreAwait();
        }
    }
}