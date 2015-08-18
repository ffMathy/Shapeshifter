using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IClipboardHookService : IHookService
    {
        event EventHandler<DataCopiedEventArgument> DataCopied;
    }
}
