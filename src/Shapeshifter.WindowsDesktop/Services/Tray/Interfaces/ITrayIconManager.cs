namespace Shapeshifter.WindowsDesktop.Services.Tray.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Infrastructure.Events;

    public interface ITrayIconManager
    {
        event EventHandler<TrayIconClickedEventArgument> IconClicked;

        void InstallTrayIcon(
            string boldMenuItemTitle,
            IEnumerable<ITrayContextMenuItem> contextMenuItems);
    }
}