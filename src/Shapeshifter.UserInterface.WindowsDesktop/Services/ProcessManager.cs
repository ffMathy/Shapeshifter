using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
        public void LaunchCommand(string command)
        {
            var process = Process.Start(command);
            processes.Add(process);
        }
        
        [ExcludeFromCodeCoverage]
        public void LaunchFile(string fileName, string arguments = null)
        {
            if(!File.Exists(fileName))
            {
                throw new ArgumentException("The given file did not exist.", nameof(fileName));
            }

            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = fileName,
                WorkingDirectory = Path.GetDirectoryName(fileName)
            });
            processes.Add(process);
        }
    }
}
