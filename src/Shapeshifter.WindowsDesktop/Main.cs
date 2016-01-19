namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;
    using Infrastructure.Environment.Interfaces;

    using Mediators.Interfaces;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;
    using Services.Tray.Interfaces;
    using Services.Updates.Interfaces;

    using Application = System.Windows.Application;

    public class Main: ISingleInstance
    {
        readonly IProcessManager processManager;
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;
        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;
        readonly IUpdateService updateService;
        readonly IEnvironmentInformation environmentInformation;
        readonly ITrayIconManager trayIconManager;

        public Main(
            IProcessManager processManager,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IAggregateArgumentProcessor aggregateArgumentProcessor,
            IUpdateService updateService,
            IEnvironmentInformation environmentInformation,
            ITrayIconManager trayIconManager)
        {
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
            this.updateService = updateService;
            this.environmentInformation = environmentInformation;
            this.trayIconManager = trayIconManager;
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

            InstallTrayIcon();

            await Update();

            LaunchMainWindow();
        }

        void InstallTrayIcon()
        {
            trayIconManager.InstallTrayIcon(
                "Settings",
                new[]
                {
                    new MenuItem(
                    "Exit",
                    (sender, e) => Application.Current.Shutdown())
                });
        }

        async Task Update()
        {
            if (!environmentInformation.GetIsDebugging() && environmentInformation.GetHasInternetAccess())
            {
                await updateService.UpdateAsync();
            }
        }

        void LaunchMainWindow()
        {
            mainWindow.SourceInitialized +=
                (sender, e) => clipboardUserInterfaceMediator.Connect(mainWindow);
            mainWindow.Show();
        }
    }
}