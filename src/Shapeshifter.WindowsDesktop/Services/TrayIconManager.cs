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
	using Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces;
	using Web.Updates.Interfaces;

    public class TrayIconManager: ITrayIconManager
    {
        readonly NotifyIcon trayIcon;

		readonly IMainThreadInvoker mainThreadInvoker;

		public event EventHandler<TrayIconDoubleClickedEventArgument> IconDoubleClicked;

        public TrayIconManager(
			IMainThreadInvoker mainThreadInvoker)
        {
            trayIcon = new NotifyIcon();
			this.mainThreadInvoker = mainThreadInvoker;
		}

        public void UpdateMenuItems(
            string boldMenuItemTitle,
            IReadOnlyCollection<MenuItem> contextMenuItems)
        {
            trayIcon.DoubleClick += (sender, e) =>
                              OnIconDoubleClicked(new TrayIconDoubleClickedEventArgument());

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(
                new MenuItem(boldMenuItemTitle, (sender, e) => OnIconDoubleClicked(new TrayIconDoubleClickedEventArgument()))
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

        protected virtual void OnIconDoubleClicked(TrayIconDoubleClickedEventArgument e)
        {
            IconDoubleClicked?.Invoke(this, e);
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
        }

		public void UpdateHoverText(string text)
		{
			mainThreadInvoker.Invoke(() => trayIcon.Text = text);
		}

		public void DisplayInformation(string title, string text)
		{
			mainThreadInvoker.Invoke(() => trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Info));
		}

		public void DisplayWarning(string title, string text)
		{
			mainThreadInvoker.Invoke(() => trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Warning));
		}

		public void DisplayError(string title, string text)
		{
			mainThreadInvoker.Invoke(() => trayIcon.ShowBalloonTip(int.MaxValue, title, text, ToolTipIcon.Error));
		}
	}
}