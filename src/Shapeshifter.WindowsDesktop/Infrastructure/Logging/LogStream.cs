namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;

    using Shared.Infrastructure.Logging.Interfaces;

    class LogStream : ILogStream
    {
        public void WriteLine(string input)
        {
            Debug.WriteLine(input);
        }
    }
}
