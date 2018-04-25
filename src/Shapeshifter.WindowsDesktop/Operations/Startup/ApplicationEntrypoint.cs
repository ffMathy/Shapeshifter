namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;
	using Serilog;

	public class ApplicationEntrypoint: ISingleInstance
    {
        readonly IStartupPreparationOperation startupPreparationOperation;
        readonly IPostPreparationOperation postPreparationOperation;
        readonly IMainWindowPreparationOperation mainWindowPreparationOperation;
		readonly ILogger logger;

		public ApplicationEntrypoint(
            IStartupPreparationOperation startupPreparationOperation,
            IPostPreparationOperation postPreparationOperation,
            IMainWindowPreparationOperation mainWindowPreparationOperation,
			ILogger logger)
        {
            this.startupPreparationOperation = startupPreparationOperation;
            this.postPreparationOperation = postPreparationOperation;
            this.mainWindowPreparationOperation = mainWindowPreparationOperation;
			this.logger = logger;
		}

        public async Task Start(
            params string[] arguments)
        {
            await Prepare(arguments);
            if (startupPreparationOperation.ShouldTerminate)
            {
				logger.Verbose("The startup preparation operation signalled a termination request. Will quit process.");
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