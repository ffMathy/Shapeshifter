namespace Shapeshifter.WindowsDesktop.Services.Tray.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using Infrastructure.Events;

    public interface ITrayIconManager: IDisposable
    {
        event EventHandler<TrayIconClickedEventArgument> IconClicked;

        void InstallTrayIcon(
            string boldMenuItemTitle,
            IReadOnlyCollection<MenuItem> contextMenuItems);
    }
}