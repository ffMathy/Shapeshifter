namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Messages.Interfaces;

    public interface IClipboardCopyInterceptor: IWindowMessageInterceptor
    {
        event EventHandler<DataCopiedEventArgument> DataCopied;

        void SkipNext();
    }
}