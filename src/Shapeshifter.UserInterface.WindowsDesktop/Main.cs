namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System.Collections.Generic;
    using System.Linq;

    using Controls.Window.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Mediators.Interfaces;

    using Services.Arguments.Interfaces;
    using Services.Interfaces;

    public class Main: ISingleInstance
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        readonly IProcessManager processManager;

        readonly IClipboardListWindow mainWindow;

        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;

        public Main(
            IEnumerable<IArgumentProcessor> argumentProcessors,
            IProcessManager processManager,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator)
        {
            this.argumentProcessors = argumentProcessors;
            this.processManager = processManager;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
        }

        public void Start(params string[] arguments)
        {
            processManager.CloseAllProcessesExceptCurrent();

            var processorsUsed = ProcessArguments(arguments);
            if (processorsUsed.Any(x => x.Terminates))
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

        IEnumerable<IArgumentProcessor> ProcessArguments(string[] arguments)
        {
            var processorsUsed = new List<IArgumentProcessor>();
            foreach (var processor in argumentProcessors)
            {
                if (!processor.CanProcess(arguments))
                {
                    continue;
                }

                processor.Process(arguments);
                processorsUsed.Add(processor);
            }

            return processorsUsed;
        }
    }
}