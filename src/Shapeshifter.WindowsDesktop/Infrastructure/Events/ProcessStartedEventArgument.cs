namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    public class ProcessStartedEventArgument : EventArgs {
        public ProcessStartedEventArgument(
            string processName)
        {
            ProcessName = processName;
        }

        public string ProcessName { get; }
    }
}