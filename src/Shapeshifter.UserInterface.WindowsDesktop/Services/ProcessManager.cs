namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        public void CloseAllProcessesExceptCurrent()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                CloseProcessesExceptProcessWithId(currentProcess.Id, processes);
            }
        }

        static void CloseProcessesExceptProcessWithId(
            int processId,
            params Process[] processes)
        {
            foreach (var process in processes)
            {
                if (process.Id == processId)
                {
                    continue;
                }

                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                }
            }
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