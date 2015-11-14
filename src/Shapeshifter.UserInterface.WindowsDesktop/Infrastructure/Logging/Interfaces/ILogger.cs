namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    internal interface ILogger
    {
        void Information(string text, int importanceFactor = 0);

        void Warning(string text);

        void Error(string text);

        void Performance(string text);
    }
}