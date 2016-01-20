namespace Shapeshifter.WindowsDesktop.Controls.Window.Factories
{
    using System;

    using Interfaces;

    using ViewModels.Interfaces;

    using Window.Interfaces;

    public class SettingsWindowFactory: ISettingsWindowFactory
    {
        readonly ISettingsViewModel viewModel;

        ISettingsWindow currentWindow;

        public SettingsWindowFactory(
            ISettingsViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public ISettingsWindow Create()
        {
            if (currentWindow != null)
            {
                return currentWindow;
            }

            return currentWindow = CreateNewWindow();
        }

        ISettingsWindow CreateNewWindow()
        {
            var newWindow = new SettingsWindow(viewModel);
            newWindow.Closed += WindowOnClosed;

            return currentWindow;
        }

        void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            currentWindow.Closed -= WindowOnClosed;

            currentWindow = null;
        }
    }
}