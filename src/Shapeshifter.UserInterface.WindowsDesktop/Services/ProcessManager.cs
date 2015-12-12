namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    using Interfaces;

    
    class ProcessManager
        : IProcessManager,
          IDisposable
    {
        readonly ICollection<Process> processes;

        public ProcessManager()
        {
            processes = new HashSet<Process>();
        }

        
        public void Dispose()
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }

        
        public void LaunchCommand(string command)
        {
            var process = Process.Start(command);
            processes.Add(process);
        }

        
        public void LaunchFile(string fileName, string arguments = null)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The given file did not exist.", nameof(fileName));
            }

            Debug.Assert(fileName != null, "fileName != null");

            var workingDirectory = Path.GetDirectoryName(fileName);
            Debug.Assert(workingDirectory != null, "workingDirectory != null");

            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory
                });
            processes.Add(process);
        }
    }
}