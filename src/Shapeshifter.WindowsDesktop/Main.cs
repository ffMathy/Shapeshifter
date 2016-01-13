namespace Shapeshifter.WindowsDesktop
{
    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Mediators.Interfaces;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;

    using System.Threading.Tasks;

    using Infrastructure.Environment.Interfaces;

    using Services.Web.Interfaces;

    public class Main: ISingleInstance
    {
        readonly IProcessManager processManager;
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;
        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;
        readonly IUpdateService updateService;
        readonly IEnvironmentInformation environmentInformation;

        public Main(
            IProcessManager processManager,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IAggregateArgumentProcessor aggregateArgumentProcessor,
            IUpdateService updateService,
            IEnvironmentInformation environmentInformation,
            IDownloader downloader)
        {
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
            this.updateService = updateService;
            this.environmentInformation = environmentInformation;
        }

        public async Task Start(
            params string[] arguments)
        {
            processManager.CloseAllProcessesExceptCurrent();

            aggregateArgumentProcessor.ProcessArguments(arguments);
            if (aggregateArgumentProcessor.ShouldTerminate)
            {
                return;
            }

            if (!environmentInformation.GetIsDebugging() && environmentInformation.GetHasInternetAccess())
            {
                await updateService.UpdateAsync();
            }

            LaunchMainWindow();
        }

        void LaunchMainWindow()
        {
            mainWindow.SourceInitialized +=
                (sender, e) => clipboardUserInterfaceMediator.Connect(mainWindow);
            mainWindow.Show();
        }
    }
}