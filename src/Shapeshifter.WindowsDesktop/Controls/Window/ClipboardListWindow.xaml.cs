namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System;
    using System.Windows;
    using System.Windows.Interop;

    using Infrastructure.Events;

    using Interfaces;

    using Native;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interfaces;

    using ViewModels.Interfaces;

    /// <summary>
    ///     Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    public partial class ClipboardListWindow
        : Window,
          IClipboardListWindow
    {
        readonly IMainWindowHandleContainer handleContainer;

        readonly IKeyInterceptor keyInterceptor;

        readonly IClipboardListViewModel viewModel;

        readonly IWindowMessageHook windowMessageHook;

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IKeyInterceptor keyInterceptor,
            IWindowMessageHook windowMessageHook,
            IMainWindowHandleContainer handleContainer)
        {
            this.handleContainer = handleContainer;
            this.keyInterceptor = keyInterceptor;
            this.viewModel = viewModel;
            this.windowMessageHook = windowMessageHook;

            SourceInitialized += ClipboardListWindow_SourceInitialized;
            Activated += ClipboardListWindow_Activated;

            InitializeComponent();

            SetupViewModel();
        }

        void ClipboardListWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;

            OnWindowHandleReady();
        }

        void SetupKeyInterception()
        {
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_KEY_UP);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_KEY_DOWN);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_KEY_LEFT);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_KEY_RIGHT);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_HOME);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                KeyboardNativeApi.VK_DELETE);
        }

        public HwndSource HandleSource => PresentationSource.FromVisual(this) as HwndSource;

        void ClipboardListWindow_Activated(object sender, EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();
        }

        void OnWindowHandleReady()
        {
            SetupKeyInterception();
            SetupWindowMessageHook();
        }

        void SetupWindowMessageHook()
        {
            windowMessageHook.Connect(this);
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
    }
}