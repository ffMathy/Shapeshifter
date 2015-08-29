using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments
{
    class CleanupArgumentProcessor : IArgumentProcessor
    {
        readonly IProcessManager processManager;

        public CleanupArgumentProcessor(
            IProcessManager processManager)
        {
            this.processManager = processManager;
        }

        public bool Terminates
            => true;

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("cleanup");
        }

        public void Process(string[] arguments)
        {
            var updateIndex = Array.IndexOf(arguments, "cleanup");
            var targetDirectory = arguments[updateIndex + 1];
            
            Directory.Delete(targetDirectory);
        }
    }
}
