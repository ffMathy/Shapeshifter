namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Interop;

    using Infrastructure.Events;

    using Interfaces;

    using ViewModels.Interfaces;

    public partial class ClipboardListWindow
        : Window,
          IClipboardListWindow
    {
        readonly IMainWindowHandleContainer handleContainer;
        readonly IClipboardListViewModel viewModel;

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IMainWindowHandleContainer handleContainer)
        {
            this.handleContainer = handleContainer;
            this.viewModel = viewModel;

            SourceInitialized += ClipboardListWindow_SourceInitialized;
            Activated += ClipboardListWindow_Activated;

            InitializeComponent();
            SetupViewModel();
        }

        void ClipboardListWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;
        }

        public HwndSource HandleSource => PresentationSource.FromVisual(this) as HwndSource;

        void ClipboardListWindow_Activated(object sender, EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();
        }

        void SetupViewModel()
        {
            viewModel.UserInterfaceShown += ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden += ViewModel_UserInterfaceHidden;

            DataContext = viewModel;
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
            viewModel.UserInterfaceShown -= ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden -= ViewModel_UserInterfaceHidden;
        }
    }
}