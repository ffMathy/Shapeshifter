#region

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    ///     Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window, IClipboardListWindow
    {
        private readonly IMainWindowHandleContainer handleContainer;
        private readonly IKeyInterceptor keyInterceptor;
        private readonly IClipboardUserInterfaceMediator mediator;
        private readonly IClipboardListViewModel viewModel;
        private readonly IWindowMessageHook windowMessageHook;

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

            SourceInitialized += ClipboardListWindow_SourceInitialized;
            Activated += ClipboardListWindow_Activated;

            InitializeComponent();

            SetupViewModel();
        }

        private void ClipboardListWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;

            OnWindowHandleReady();
        }

        private void SetupKeyInterception()
        {
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_UP);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_DOWN);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_LEFT);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_KEY_RIGHT);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_HOME);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle, KeyboardApi.VK_DELETE);
        }

        public HwndSource HandleSource
        {
            get { return PresentationSource.FromVisual(this) as HwndSource; }
        }

        private void ClipboardListWindow_Activated(object sender, EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();
        }

        private void OnWindowHandleReady()
        {
            SetupKeyInterception();
            SetupWindowMessageHook();
        }

        private void SetupWindowMessageHook()
        {
            windowMessageHook.Connect(this);
        }

        private void SetupViewModel()
        {
            viewModel.UserInterfaceShown += ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden += ViewModel_UserInterfaceHidden;

            DataContext = viewModel;
        }

        private void ViewModel_UserInterfaceHidden(
            object sender,
            UserInterfaceHiddenEventArgument e)
        {
            Hide();
        }

        private void ViewModel_UserInterfaceShown(
            object sender,
            UserInterfaceShownEventArgument e)
        {
            Show();
        }
    }
}