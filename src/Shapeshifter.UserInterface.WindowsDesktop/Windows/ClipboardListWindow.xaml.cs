using System.Windows;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Windows.Interop;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window, IClipboardListWindow
    {
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyInterceptor keyInterceptor;
        readonly IClipboardUserInterfaceMediator mediator;
        readonly IClipboardListViewModel viewModel;
        readonly IWindowMessageHook windowMessageHook;

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IKeyInterceptor keyInterceptor,
            IWindowMessageHook windowMessageHook,
            IMainWindowHandleContainer handleContainer,
            IClipboardUserInterfaceMediator mediator)
        {
            this.mediator = mediator;
            this.handleContainer = handleContainer;
            this.keyInterceptor = keyInterceptor;
            this.viewModel = viewModel;
            this.windowMessageHook = windowMessageHook;

            Activated += ClipboardListWindow_Activated;

            InitializeComponent();

            SetupViewModel();
        }

        void SetupKeyInterception()
        {
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_UP);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_DOWN);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_LEFT);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_RIGHT);
        }

        public HwndSource HandleSource
        {
            get
            {
                return PresentationSource.FromVisual(this) as HwndSource;
            }
        }

        void ClipboardListWindow_Activated(object sender, System.EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();

            handleContainer.Handle = HandleSource.Handle;

            OnWindowHandleReady();
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
