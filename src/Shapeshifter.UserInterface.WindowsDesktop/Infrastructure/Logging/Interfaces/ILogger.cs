namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    interface ILogger
    {
        void Information(string text);

        void Warning(string text);

        void Error(string text);
    }
}
