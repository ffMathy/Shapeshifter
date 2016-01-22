namespace Shapeshifter.WindowsDesktop.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;

    public class ApplicationEntrypoint: ISingleInstance
    {
        readonly IStartupPreparationOperation startupPreparationOperation;
        readonly IPostPreparationOperation postPreparationOperation;
        readonly IMainWindowPreparationOperation mainWindowPreparationOperation;

        public ApplicationEntrypoint(
            IStartupPreparationOperation startupPreparationOperation,
            IPostPreparationOperation postPreparationOperation,
            IMainWindowPreparationOperation mainWindowPreparationOperation)
        {
            this.startupPreparationOperation = startupPreparationOperation;
            this.postPreparationOperation = postPreparationOperation;
            this.mainWindowPreparationOperation = mainWindowPreparationOperation;
        }

        public async Task Start(
            params string[] arguments)
        {
            await Prepare(arguments);
            if (startupPreparationOperation.ShouldTerminate)
            {
                return;
            }

            await postPreparationOperation.RunAsync();
            await mainWindowPreparationOperation.RunAsync();
        }

        async Task Prepare(string[] arguments)
        {
            startupPreparationOperation.Arguments = arguments;
            await startupPreparationOperation.RunAsync();
        }
    }
}