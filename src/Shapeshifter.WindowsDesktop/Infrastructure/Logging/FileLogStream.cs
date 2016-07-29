namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Interfaces;

    using Services.Files.Interfaces;

    class FileLogStream: ILogStream
    {
        readonly IFileManager fileManager;

        readonly string logFileName;

        public FileLogStream(
            IFileManager fileManager)
        {
            this.fileManager = fileManager;
            logFileName = fileManager.WriteBytesToTemporaryFile("Shapeshifter.log", new byte[0]);
        }

        public void WriteLine(string input)
        {
            fileManager.AppendLineToFile(logFileName, input);
        }
    }
}