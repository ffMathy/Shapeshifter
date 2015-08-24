using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System.Windows.Interop;
using System.Windows;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class WindowMessageHook : IWindowMessageHook, IDisposable
    {
        HwndSource hooker;

        readonly IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors;

        readonly ILogger logger;

        public bool IsConnected
        {
            get; private set;
        }

        public IntPtr MainWindowHandle
            => hooker.Handle;

        public WindowMessageHook(
            IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors,
            ILogger logger)
        {
            this.windowMessageInterceptors = windowMessageInterceptors;
            this.logger = logger;

            logger.Information($"Window message hook was constructed using {windowMessageInterceptors.Count()} interceptors.");
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

        void UninstallWindowMessageHook()
        {
            hooker.RemoveHook(WindowHookCallback);
        }

        void UninstallInterceptors()
        {
            var mainWindowHandle = hooker.Handle;
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

        void InstallInterceptors()
        {
            var mainWindowHandle = hooker.Handle;
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Install(mainWindowHandle);
                logger.Information($"Installed interceptor {interceptor.GetType().Name}.");
            }
        }

        static void EnsureWindowIsPresent()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Can't install a window hook when there is no window open.");
            }
        }

        void InstallWindowMessageHook()
        {
            var hooker = FetchHandleSource();
            hooker.AddHook(WindowHookCallback);

            logger.Information($"Installed message hook.");

            this.hooker = hooker;
        }

        static HwndSource FetchHandleSource()
        {
            var hooker = PresentationSource.FromVisual(Application.Current.MainWindow) as HwndSource;
            if (hooker == null)
            {
                throw new InvalidOperationException("Could not fetch the handle of the main window.");
            }

            return hooker;
        }

        IntPtr WindowHookCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            logger.Information($"Message received: [{hwnd}, {msg}, {wParam}, {lParam}]");

            foreach (var interceptor in windowMessageInterceptors)
            {
                var argument = new WindowMessageReceivedArgument(hwnd, msg, wParam, lParam);
                interceptor.ReceiveMessageEvent(argument);

                logger.Information($"Message passed to interceptor {interceptor.GetType().Name}.");

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
