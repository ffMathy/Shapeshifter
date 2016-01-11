namespace Shapeshifter.WindowsDesktop.Mediators.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Services.Interfaces;

    public interface IClipboardUserInterfaceMediator: IHookService
    {
        event EventHandler<ControlEventArgument> ControlAdded;

        event EventHandler<ControlEventArgument> ControlRemoved;

        event EventHandler<ControlEventArgument> ControlPinned;

        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;

        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;

        event EventHandler<PastePerformedEventArgument> PastePerformed;

        IEnumerable<IClipboardDataControlPackage> ClipboardElements { get; }

        void Cancel();
    }
}