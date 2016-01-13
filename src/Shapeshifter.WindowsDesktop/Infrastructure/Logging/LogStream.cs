namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;
    using System.IO;

    using Interfaces;

    using Services.Files.Interfaces;

    class LogStream : ILogStream
    {
        readonly IFileManager fileManager;

        string logFilePath;

        public LogStream(
            IFileManager fileManager)
        {
            this.fileManager = fileManager;
            Prepare();
        }

        void Prepare()
        {
            var folder = fileManager.PrepareFolder();
            logFilePath = Path.Combine(folder, "Application.log");
        }

        public void WriteLine(string input)
        {
            Debug.WriteLine(input);
            File.AppendAllLines(logFilePath, new [] {input});
        }
    }
}
