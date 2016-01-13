namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;
    using System.IO;

    using Environment.Interfaces;

    using Interfaces;

    using Services.Files.Interfaces;

    class LogStream : ILogStream
    {
        readonly IFileManager fileManager;
        readonly IEnvironmentInformation environmentInformation;

        string logFilePath;

        public LogStream(
            IFileManager fileManager,
            IEnvironmentInformation environmentInformation)
        {
            this.fileManager = fileManager;
            this.environmentInformation = environmentInformation;
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

            if (environmentInformation.GetIsInDesignTime())
            {
                return;
            }

            lock (this)
            {
                File.AppendAllLines(
                    logFilePath,
                    new[]
                    {
                        input
                    });
            }
        }
    }
}
