using System;
using System.IO;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments
{
    internal class CleanupArgumentProcessor : IArgumentProcessor
    {
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