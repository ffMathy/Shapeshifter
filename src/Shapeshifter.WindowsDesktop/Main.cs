namespace Shapeshifter.WindowsDesktop
{
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;
    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Events;

    using Mediators.Interfaces;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;
    using Services.Tray.Interfaces;
    using Services.Updates.Interfaces;

    using Application = System.Windows.Application;
    using Controls.Window;
    using Controls.Window.Factories.Interfaces;

    public class Main: ISingleInstance
    {
        readonly IProcessManager processManager;
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;
        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;
        readonly IUpdateService updateService;
        readonly IEnvironmentInformation environmentInformation;
        readonly ITrayIconManager trayIconManager;
        readonly ISettingsWindowFactory settingsWindowFactory;

        public Main(
            IProcessManager processManager,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IAggregateArgumentProcessor aggregateArgumentProcessor,
            IUpdateService updateService,
            IEnvironmentInformation environmentInformation,
            ITrayIconManager trayIconManager,
            ISettingsWindowFactory settingsWindowFactory)
        {
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
            this.updateService = updateService;
            this.environmentInformation = environmentInformation;
            this.trayIconManager = trayIconManager;
            this.settingsWindowFactory = settingsWindowFactory;
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
            trayIconManager.IconClicked += TrayIconManager_IconClicked;
            trayIconManager.InstallTrayIcon(
                "Settings",
                new[]
                {
                    new MenuItem(
                    "Exit",
                    (sender, e) => Application.Current.Shutdown())
                });
        }

        void TrayIconManager_IconClicked(object sender, TrayIconClickedEventArgument e)
        {
            var window = settingsWindowFactory.Create();
            window.Show();
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