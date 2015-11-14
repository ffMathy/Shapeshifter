using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces
{
    public interface IClipboardUserInterfaceMediator : IHookService
    {
        event EventHandler<ControlEventArgument> ControlAdded;
        event EventHandler<ControlEventArgument> ControlRemoved;
        event EventHandler<ControlEventArgument> ControlPinned;
        event EventHandler<ControlEventArgument> ControlHighlighted;

        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        IEnumerable<IClipboardDataControlPackage> ClipboardElements { get; }
    }
}
