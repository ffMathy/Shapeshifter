using System;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class HookHostCommunicator: MarshalByRefObject
    {
        static bool ignore;

        public static void SetShouldIgnoreHook(bool newIgnore)
        {
            ignore = newIgnore;
        }

        public bool GetShouldIgnoreHook()
        {
            return ignore;
        }

        public void ReportException(Exception exception)
        {
            throw exception;
        }

        public void DebugWriteLine(string message)
        {
            message = ">> " + message;

            Console.WriteLine(message);
            Trace.WriteLine(message);
        }

        public void Ping() { }
    }
}
