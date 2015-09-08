using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    class Main : ISingleInstance
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
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        static void CloseProcessesExceptProcessWithId(int processId, params Process[] processes)
        {
            foreach (var process in processes)
            {
                if (process.Id != processId)
                {
                    process.CloseMainWindow();
                    process.WaitForExit();
                }
            }
        }

        void LaunchMainWindow()
        {
            mainWindow.SourceInitialized += (sender, e) => clipboardUserInterfaceMediator.Connect(mainWindow);
            mainWindow.Show();
        }

        IEnumerable<IArgumentProcessor> ProcessArguments(string[] arguments)
        {
            var processorsUsed = new List<IArgumentProcessor>();
            foreach (var processor in argumentProcessors)
            {
                if (processor.CanProcess(arguments))
                {
                    processor.Process(arguments);
                    processorsUsed.Add(processor);
                }
            }

            return processorsUsed;
        }
    }
}
