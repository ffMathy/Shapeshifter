using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Windows.Interop;
using System.Windows;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class WindowMessageHook : IWindowMessageHook, IDisposable
    {
        private HwndSource hooker;
        private readonly IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors;

        public bool IsConnected
        {
            get; private set;
        }

        private IntPtr MainWindowHandle
        {
            get
            {
                var interopHelper = new WindowInteropHelper(App.Current.MainWindow);
                var windowHandle = interopHelper.EnsureHandle();
                return windowHandle;
            }
        }

        public WindowMessageHook(
            IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors)
        {
            this.windowMessageInterceptors = windowMessageInterceptors;
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                UninstallInterceptors();
                UninstallWindowMessageHook();

                IsConnected = false;
            }
        }

        private void UninstallWindowMessageHook()
        {
            hooker.RemoveHook(WindowHookCallback);
        }

        private void UninstallInterceptors()
        {
            var mainWindowHandle = MainWindowHandle;
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Uninstall(mainWindowHandle);
            }
        }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The window message hook has already been connected.");
            }

            EnsureWindowIsPresent();

            InstallWindowMessageHook();
            InstallInterceptors();

            IsConnected = true;
        }

        private void InstallInterceptors()
        {
            var mainWindowHandle = MainWindowHandle;
            foreach(var interceptor in windowMessageInterceptors)
            {
                interceptor.Install(mainWindowHandle);
            }
        }

        private static void EnsureWindowIsPresent()
        {
            var mainWindow = App.Current.MainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Can't install a window hook when there is no window open.");
            }
        }

        private void InstallWindowMessageHook()
        {
            var hooker = FetchHandleSource();
            hooker.AddHook(WindowHookCallback);

            this.hooker = hooker;
        }

        private static HwndSource FetchHandleSource()
        {
            var hooker = PresentationSource.FromVisual(App.Current.MainWindow) as HwndSource;
            if (hooker == null)
            {
                throw new InvalidOperationException("Could not fetch the handle of the main window.");
            }

            return hooker;
        }

        private IntPtr WindowHookCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            foreach (var interceptor in windowMessageInterceptors)
            {
                var argument = new WindowMessageReceivedArgument(hwnd, msg, wParam, lParam);
                interceptor.ReceiveMessageEvent(argument);

                handled |= argument.Handled;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (hooker != null)
            {
                hooker.Dispose();
            }
        }
    }
}
