namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Windows.Interfaces;

    using Infrastructure.Dependencies.Interfaces;

    using Mediators.Interfaces;

    using Services.Arguments.Interfaces;

    
    class Main: ISingleInstance
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        readonly IClipboardListWindow mainWindow;

        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;

        public Main(
            IEnumerable<IArgumentProcessor> argumentProcessors,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator)
        {
            this.argumentProcessors = argumentProcessors;
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
        }

        public void Start(string[] arguments)
        {
            CloseAllProcessesExceptCurrent();

            var processorsUsed = ProcessArguments(arguments);
            if (processorsUsed.Any(x => x.Terminates))
            {
                return;
            }

            LaunchMainWindow();
        }

        static void CloseAllProcessesExceptCurrent()
        {
            //TODO: if this method gets isolated, the whole class can be unit tested.
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        static void CloseProcessesExceptProcessWithId(
            int processId,
            params Process[] processes)
        {
            foreach (var process in processes)
            {
                if (process.Id == processId)
                {
                    continue;
                }

                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                }
            }
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