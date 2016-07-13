using System;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    using System.Diagnostics;

    public class HookHostCommunicator: MarshalByRefObject
    {
        public void ReportException(Exception exception)
        {
            throw exception;
        }

        public void DebugWriteLine(string message)
        {
            Console.WriteLine(message);
            Trace.WriteLine(message);
        }

        public void Ping() { }
    }
}
