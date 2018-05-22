namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	using System;
	using System.Windows;
	using System.Windows.Interop;

	using Infrastructure.Events;

	using Interfaces;

	using ViewModels.Interfaces;
	using System.Diagnostics.CodeAnalysis;
	using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
	using System.Windows.Media;

	using Serilog;

	[ExcludeFromCodeCoverage]
    public partial class MainWindow
        : Window,
          IMainWindow
    {
        readonly IMainWindowHandleContainer handleContainer;
		readonly ILogger logger;
		readonly IMainViewModel viewModel;

        public MainWindow(
            IMainViewModel viewModel,
            IMainWindowHandleContainer handleContainer,
			IEnvironmentInformation environmentInformation,
			ILogger logger)
        {
            this.handleContainer = handleContainer;
			this.logger = logger;
			this.viewModel = viewModel;

			Left = int.MinValue;
			Top = int.MinValue;
			Width = 1;
			Height = 1;

            SourceInitialized += MainWindow_SourceInitialized;

            InitializeComponent();
            SetupViewModel();

			if(environmentInformation.GetIsDebugging())
				Background = Brushes.Transparent;
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;
		}

        public HwndSource HandleSource => PresentationSource.FromVisual(this) as HwndSource;

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
			logger.Information("Hiding user interface.");
			Hide();
        }

        void ViewModel_UserInterfaceShown(
            object sender,
            UserInterfaceShownEventArgument e)
		{
			logger.Information("Showing user interface.");

			Left = viewModel.ActiveScreen.WorkingArea.X;
			Top = viewModel.ActiveScreen.WorkingArea.Y;
			Width = viewModel.ActiveScreen.WorkingArea.Width;
			Height = viewModel.ActiveScreen.WorkingArea.Height;
			
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