using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IClipboardHookService : IHookService
    {
        event EventHandler<DataCopiedEventArgument> DataCopied;
    }
}
