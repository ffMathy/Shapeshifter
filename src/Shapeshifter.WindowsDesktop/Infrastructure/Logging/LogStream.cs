namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;
    using System.IO;

    using Environment.Interfaces;

    using Interfaces;

    using Services.Files.Interfaces;

    class LogStream: ILogStream
    {
        public void WriteLine(string input)
        {
            Debug.WriteLine(input);
        }
    }
}