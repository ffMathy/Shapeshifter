namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using Infrastructure.Events;

    using Interfaces;

    using Properties;

    using Web.Updates.Interfaces;

    public class TrayIconManager: ITrayIconManager
    {
        readonly IUpdateService updateService;

        readonly NotifyIcon trayIcon;

        public event EventHandler<TrayIconClickedEventArgument> IconClicked;

        public TrayIconManager(
            IUpdateService updateService)
        {
            trayIcon = new NotifyIcon();
            this.updateService = updateService;
        }

        public void InstallTrayIcon(
            string boldMenuItemTitle,
            IReadOnlyCollection<MenuItem> contextMenuItems)
        {
            trayIcon.Click += (sender, e) =>
                              OnIconClicked(new TrayIconClickedEventArgument());

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(
                new MenuItem(boldMenuItemTitle)
                {
                    DefaultItem = true,
                    BarBreak = true
                });
            contextMenu
                .MenuItems
                .AddRange(contextMenuItems.ToArray());

            trayIcon.Icon = Resources.ShapeshifterIcon;
            trayIcon.ContextMenu = contextMenu;
            trayIcon.Text = "Shapeshifter version " + updateService.GetCurrentVersion();
            trayIcon.Visible = true;
        }

        protected virtual void OnIconClicked(TrayIconClickedEventArgument e)
        {
            IconClicked?.Invoke(this, e);
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
        }
    }
}