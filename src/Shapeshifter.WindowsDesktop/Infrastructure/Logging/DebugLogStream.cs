namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;

    using Interfaces;

    class DebugLogStream: ILogStream
    {
        public void WriteLine(string input)
        {
            Debug.WriteLine(input);
        }
    }
}