using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    class Main : ISingleInstance
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator mediator;
        readonly IWindowMessageHook windowMessageHook;

        public Main(
            IEnumerable<IArgumentProcessor> argumentProcessors,
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator mediator,
            IWindowMessageHook windowMessageHook)
        {
            this.argumentProcessors = argumentProcessors;
            this.mainWindow = mainWindow;
            this.mediator = mediator;
            this.windowMessageHook = windowMessageHook;
        }

        public void Start(string[] arguments)
        {
            var processorsUsed = ProcessArguments(arguments);
            if (processorsUsed.Any(x => x.Terminates))
            {
                return;
            }

            Run();
        }

        void Run()
        {
            mainWindow.Show();
            windowMessageHook.Connect();
            mediator.Connect();
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
