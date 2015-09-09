using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System.Diagnostics;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging
{
    class Logger : ILogger
    {
        const int MinimumImportanceFactor = 0;

        void Log(string text)
        {
            Debug.WriteLine($"{text}");
        }

        public void Error(string text)
        {
            Log("Error: " + text);
        }

        public void Performance(string text)
        {
            Log("Performance information: " + text);
        }

        public void Information(
            string text, int importanceFactor = 0)
        {
            if (importanceFactor >= MinimumImportanceFactor)
            {
                Log("Information: " + text);
            }
        }

        public void Warning(string text)
        {
            Log("Warning: " + text);
        }
    }
}
