namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    interface ILogger
    {
        void Information(string text, int importanceFactor = 0);

        void Warning(string text);

        void Error(string text);

        void Performance(string text);
    }
}