namespace Shapeshifter.WindowsDesktop.Mediators.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Data.Interfaces;

    using Infrastructure.Events;

    using Services.Interfaces;

    public interface IClipboardUserInterfaceInteractionMediator
        : IHookService
    {
        event EventHandler<PackageEventArgument> PackageAdded;
        event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
        event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;
        event EventHandler<PastePerformedEventArgument> PastePerformed;
        event EventHandler SelectedNextItem;
        event EventHandler SelectedPreviousItem;
        event EventHandler PaneSwapped;
        event EventHandler RemovedCurrentItem;

        ClipboardUserInterfacePane CurrentPane { get; set; }

        IEnumerable<IClipboardDataControlPackage> ClipboardElements { get; }

        void Cancel();
    }
}