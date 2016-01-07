namespace Shapeshifter.WindowsDesktop.Shared.Infrastructure.Logging.Interfaces
{
    using Dependencies.Interfaces;

    using Handles.Interfaces;

    public interface ILogger: ISingleInstance
    {
        void Information(string text, int importanceFactor = 0);

        void Warning(string text);

        void Error(string text);

        void Performance(string text);

        void PrintStackTrace();

        IIndentationHandle Indent();
    }
}