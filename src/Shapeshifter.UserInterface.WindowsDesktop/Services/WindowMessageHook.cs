using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Windows.Interop;
using System.Windows;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class WindowMessageHook : IWindowMessageHook, IDisposable
    {
        private HwndSource hooker;
        private IntPtr mainWindowHandle;

        public event EventHandler<WindowMessageReceivedArgument> MessageReceived;

        public bool IsConnected
        {
            get; private set;
        }

        public IntPtr MainWindowHandle
        {
            get
            {
                if(mainWindowHandle == default(IntPtr))
                {
                    throw new InvalidOperationException("The window message hook has not been installed yet.");
                }

                return mainWindowHandle;
            }
            private set
            {
                mainWindowHandle = value;
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                hooker.RemoveHook(WindowHookCallback);

                IsConnected = false;
            }
        }

        private static IntPtr FetchWindowHandle(Window mainWindow)
        {
            var interopHelper = new WindowInteropHelper(mainWindow);
            var windowHandle = interopHelper.EnsureHandle();
            return windowHandle;
        }

        public void Connect()
        {
            EnsureWindowIsPresent();

            if (!IsConnected)
            {
                FetchMainWindowHandle();
                InstallWindowMessageHook();

                IsConnected = true;
            }
        }

        private void FetchMainWindowHandle()
        {
            MainWindowHandle = FetchWindowHandle(App.Current.MainWindow);
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
            if (MessageReceived != null)
            {
                var argument = new WindowMessageReceivedArgument(hwnd, msg, wParam, lParam);
                MessageReceived(this, argument);
                handled = argument.Handled;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if(hooker != null)
            {
                hooker.Dispose();
            }
        }
    }
}
