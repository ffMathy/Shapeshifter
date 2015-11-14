#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces
{
    public interface IClipboardCopyInterceptor : IWindowMessageInterceptor
    {
        event EventHandler<DataCopiedEventArgument> DataCopied;

        void SkipNext();
    }
}