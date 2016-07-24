namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Interfaces;

    using Services.Files.Interfaces;

    class FileLogStream: ILogStream
    {
        const string logFileName = "Shapeshifter.log";

        public FileLogStream()
        {
            File.WriteAllText(logFileName, string.Empty);
        }

        public void WriteLine(string input)
        {
            File.AppendAllLines(logFileName, new [] { input });
        }
    }
}