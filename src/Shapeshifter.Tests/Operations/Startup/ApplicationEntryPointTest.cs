namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Interfaces;

    [TestClass]
    public class ApplicationEntryPointTest: UnitTestFor<ApplicationEntrypoint>
    {
        [TestMethod]
        public async Task CanStartWithTerminatingArgumentProcessorShowsMainWindow()
        {
            Container
                .Resolve<IStartupPreparationOperation>()
                .ShouldTerminate.Returns(true);
            
            await SystemUnderTest.Start();

            var fakeWindow = Container.Resolve<IMainWindowPreparationOperation>();
            fakeWindow.DidNotReceive()
                      .RunAsync()
                      .IgnoreAwait();
        }
    }
}