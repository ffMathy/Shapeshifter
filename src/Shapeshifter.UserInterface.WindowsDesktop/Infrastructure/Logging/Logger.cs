using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System.Diagnostics;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging
{
    class Logger : ILogger
    {
        void Log(string text)
        {
            Debug.WriteLine($"{text}");
        }

        public void Error(string text)
        {
            Log("Error: " + text);
        }

        public void Information(string text)
        {
            Log("Information: " + text);
        }

        public void Warning(string text)
        {
            Log("Warning: " + text);
        }
    }
}
