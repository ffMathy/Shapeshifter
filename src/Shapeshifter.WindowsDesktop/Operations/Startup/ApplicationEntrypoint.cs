namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    using Interfaces;

    public class ApplicationEntrypoint: ISingleInstance
    {
        readonly IPreparationOperation preparationOperation;
        readonly IPostPreparationOperation postPreparationOperation;
        readonly IMainWindowPreparationOperation mainWindowPreparationOperation;

        public ApplicationEntrypoint(
            IPreparationOperation preparationOperation,
            IPostPreparationOperation postPreparationOperation,
            IMainWindowPreparationOperation mainWindowPreparationOperation)
        {
            this.preparationOperation = preparationOperation;
            this.postPreparationOperation = postPreparationOperation;
            this.mainWindowPreparationOperation = mainWindowPreparationOperation;
        }

        public async Task Start(
            params string[] arguments)
        {
            await Prepare(arguments);
            if (preparationOperation.ShouldTerminate)
            {
                return;
            }

            await postPreparationOperation.RunAsync();
            await mainWindowPreparationOperation.RunAsync();
        }

        async Task Prepare(string[] arguments)
        {
            preparationOperation.Arguments = arguments;
            await preparationOperation.RunAsync();
        }
    }
}