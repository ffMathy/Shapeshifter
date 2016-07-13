using System;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    public class HookHostCommunicator: MarshalByRefObject
    {
        public void ReportException(Exception exception)
        {
            throw exception;
        }

        public void Ping() { }
    }
}
