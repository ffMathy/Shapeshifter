using System.Diagnostics;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging
{
    internal class Logger : ILogger
    {
        private const int MinimumImportanceFactor = 0;

        private static void Log(string text)
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