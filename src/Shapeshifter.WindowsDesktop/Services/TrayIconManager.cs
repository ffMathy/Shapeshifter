namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using Infrastructure.Events;

    using Interfaces;

    using Properties;
	using Serilog;
	using Web.Updates.Interfaces;

    public class TrayIconManager: ITrayIconManager
    {
        readonly NotifyIcon trayIcon;

        public event EventHandler<TrayIconClickedEventArgument> IconClicked;

        public TrayIconManager(
			ILogger logger)
        {
            trayIcon = new NotifyIcon();
        }

        public void UpdateMenuItems(
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
            trayIcon.Text = "Shapeshifter version " + Program.GetCurrentVersion();
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

		public void UpdateHoverText(string text)
		{
			trayIcon.Text = text;
		}

		public void DisplayInformation(string title, string text)
		{
			trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Info);
		}

		public void DisplayWarning(string title, string text)
		{
			trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Warning);
		}

		public void DisplayError(string title, string text)
		{
			trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Error);
		}
	}
}