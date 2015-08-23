using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    class Main : ISingleInstance
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        readonly IClipboardListWindow mainWindow;

        public Main(
            IClipboardListWindow mainWindow,
            IEnumerable<IArgumentProcessor> argumentProcessors)
        {
            this.argumentProcessors = argumentProcessors;
            this.mainWindow = mainWindow;
        }

        public void Start(string[] arguments)
        {
            var processorsUsed = ProcessArguments(arguments);
            if (processorsUsed.Any(x => x.Terminates))
            {
                return;
            }

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
