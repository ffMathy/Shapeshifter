namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    using Dependencies.Interfaces;

    using Handles.Interfaces;
    using System;

    public interface ILogger: ISingleInstance
    {
        void Information(string text, int importanceFactor = 0);

        void Warning(string text);

        void Error(string text);
        void Error(Exception exception);

        void Performance(string text);

        void PrintStackTrace();

        IIndentationHandle Indent();
    }
}