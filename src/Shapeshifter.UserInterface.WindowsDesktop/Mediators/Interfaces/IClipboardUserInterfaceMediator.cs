using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IClipboardUserInterfaceMediator : IHookService
    {
        event EventHandler<ControlEventArgument> ControlAdded;
        event EventHandler<ControlEventArgument> ControlRemoved;
        event EventHandler<ControlEventArgument> ControlPinned;
        event EventHandler<ControlEventArgument> ControlHighlighted;

        IEnumerable<IClipboardDataControlPackage> ClipboardElements { get; }
    }
}
