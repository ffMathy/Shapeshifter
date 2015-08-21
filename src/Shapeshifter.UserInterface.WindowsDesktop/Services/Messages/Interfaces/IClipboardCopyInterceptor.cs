using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IClipboardCopyInterceptor : IWindowMessageInterceptor
    {
        event EventHandler<DataCopiedEventArgument> DataCopied;
    }
}
