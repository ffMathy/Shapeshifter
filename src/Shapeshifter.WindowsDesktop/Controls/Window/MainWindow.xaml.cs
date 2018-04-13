namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System;
    using System.Windows;
    using System.Windows.Interop;

    using Infrastructure.Events;

    using Interfaces;

    using Mediators.Interfaces;

    using ViewModels.Interfaces;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public partial class MainWindow
        : Window,
          IMainWindow
    {
        readonly IMainWindowHandleContainer handleContainer;
        readonly IMainViewModel viewModel;

        public MainWindow(
            IMainViewModel viewModel,
            IMainWindowHandleContainer handleContainer)
        {
            this.handleContainer = handleContainer;
            this.viewModel = viewModel;

            SourceInitialized += MainWindow_SourceInitialized;
            Activated += MainWindow_Activated;

            InitializeComponent();
            SetupViewModel();
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;
		}

        public HwndSource HandleSource => PresentationSource.FromVisual(this) as HwndSource;

        void MainWindow_Activated(object sender, EventArgs e)
		{
			Activated -= MainWindow_Activated;
            Hide();
        }

        void SetupViewModel()
        {
            viewModel.UserInterfaceViewModel.UserInterfaceShown += ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceViewModel.UserInterfaceHidden += ViewModel_UserInterfaceHidden;

            DataContext = viewModel;

			UserInterfaceControl.Initialize(viewModel.UserInterfaceViewModel);
        }

        void ViewModel_UserInterfaceHidden(
            object sender,
            UserInterfaceHiddenEventArgument e)
        {
            Hide();
        }

        void ViewModel_UserInterfaceShown(
            object sender,
            UserInterfaceShownEventArgument e)
        {
            Show();
        }

        public void AddHwndSourceHook(HwndSourceHook hook)
        {
            HandleSource.AddHook(hook);
        }

        public void RemoveHwndSourceHook(HwndSourceHook hook)
        {
            HandleSource.RemoveHook(hook);
        }

        public void Dispose()
        {
            viewModel.UserInterfaceViewModel.UserInterfaceShown -= ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceViewModel.UserInterfaceHidden -= ViewModel_UserInterfaceHidden;

			UserInterfaceControl.Dispose();
        }
    }
}