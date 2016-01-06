namespace Shapeshifter.WindowsDesktop
{
    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Mediators.Interfaces;

    using Shared.Services.Arguments.Interfaces;
    using Shared.Services.Interfaces;

    public class Main: ISingleInstance
    {
        readonly IProcessManager processManager;

        readonly IClipboardListWindow mainWindow;

        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;

        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;

        public Main(
            IProcessManager processManager,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator,
            IAggregateArgumentProcessor aggregateArgumentProcessor)
        {
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
        }

        public void Start(params string[] arguments)
        {
            processManager.CloseAllProcessesExceptCurrent();

            aggregateArgumentProcessor.ProcessArguments(arguments);
            if (aggregateArgumentProcessor.ShouldTerminate)
            {
                return;
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