namespace Shapeshifter.UserInterface.WindowsDesktop.Installer
{
    using Shapeshifter.WindowsDesktop.Shared.Controls.Window.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Infrastructure.Dependencies.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Services.Arguments.Interfaces;
    using Shapeshifter.WindowsDesktop.Shared.Services.Interfaces;

    public class Main: ISingleInstance
    {
        readonly IProcessManager processManager;

        readonly IWindow mainWindow;

        readonly IAggregateArgumentProcessor aggregateArgumentProcessor;

        public Main(
            IProcessManager processManager,
            IWindow mainWindow,
            IAggregateArgumentProcessor aggregateArgumentProcessor)
        {
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.aggregateArgumentProcessor = aggregateArgumentProcessor;
        }

        public void Start(
            params string[] arguments)
        {
            processManager.CloseAllProcessesExceptCurrent();

            aggregateArgumentProcessor.ProcessArguments(arguments);
            if (aggregateArgumentProcessor.ShouldTerminate)
            {
                return;
            }

            mainWindow.Show();
        }
    }
}