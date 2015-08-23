using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class ProcessManager : IProcessManager, IDisposable
    {
        readonly ICollection<Process> processes;

        public ProcessManager()
        {
            this.processes = new HashSet<Process>();
        }

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            foreach(var process in processes)
            {
                process.Dispose();
            }
        }

        [ExcludeFromCodeCoverage]
        public void StartProcess(string fileName, string arguments = null)
        {
            var process = Process.Start(fileName, arguments ?? string.Empty);
            processes.Add(process);
        }
    }
}
