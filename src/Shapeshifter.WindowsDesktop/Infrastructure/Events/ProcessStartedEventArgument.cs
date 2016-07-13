namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    public class ProcessStartedEventArgument : EventArgs {
        public ProcessStartedEventArgument(
            string processName,
            int processId)
        {
            ProcessName = processName;
            ProcessId = processId;
        }

        public string ProcessName { get; }
        public int ProcessId { get;}
    }
}