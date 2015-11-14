#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    internal class Main : ISingleInstance
    {
        private readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        private readonly IClipboardListWindow mainWindow;
        private readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;

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

        private static void CloseAllProcessesExceptCurrent()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        private static void CloseProcessesExceptProcessWithId(int processId, params Process[] processes)
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

        private void LaunchMainWindow()
        {
            mainWindow.SourceInitialized += (sender, e) => clipboardUserInterfaceMediator.Connect(mainWindow);
            mainWindow.Show();
        }

        private IEnumerable<IArgumentProcessor> ProcessArguments(string[] arguments)
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