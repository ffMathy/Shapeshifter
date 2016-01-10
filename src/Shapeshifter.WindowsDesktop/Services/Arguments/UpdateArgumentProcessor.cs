using WindowsProcess = System.Diagnostics.Process;

namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System.Linq;

    using Interfaces;

    class UpdateArgumentProcessor: IArgumentProcessor
    {
        public bool Terminates
            => false;

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("update");
        }

        public void Process(string[] arguments)
        {
        }
    }
}