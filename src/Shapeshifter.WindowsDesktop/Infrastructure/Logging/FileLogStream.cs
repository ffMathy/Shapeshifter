namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Interfaces;

    using Services.Files.Interfaces;

    class FileLogStream: ILogStream
    {
        string logFileName;

        public IFileManager FileManager { get; set; }

        public void WriteLine(string input)
        {
            if (logFileName == null)
            {
                logFileName = FileManager.WriteBytesToTemporaryFile("Shapeshifter.log", new byte[0]);
            }
            FileManager.AppendLineToFile(logFileName, input);
        }
    }
}