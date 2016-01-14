namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;

    using Infrastructure.Events;
    using System.Collections.Generic;

    public interface ITrayIconManager
    {
        event EventHandler<TrayIconClickedEventArgument> IconClicked;

        void InstallTrayIcon(
            string boldTitle,
            IEnumerable<IContextMenuItem> contextMenuItems);
    }
}