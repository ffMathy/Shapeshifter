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
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System.Windows.Media;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class WindowMessageHook : IWindowMessageHook, IDisposable
    {
        HwndSource hooker;

        readonly IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors;

        readonly ILogger logger;
        readonly IClipboardListWindow mainWindow;

        public bool IsConnected
        {
            get; private set;
        }

        private IntPtr MainWindowHandle
            => hooker.Handle;

        public WindowMessageHook(
            IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors,
            ILogger logger,
            IClipboardListWindow mainWindow)
        {
            this.windowMessageInterceptors = windowMessageInterceptors;
            this.logger = logger;
            this.mainWindow = mainWindow;

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

        void InstallWindowMessageHook()
        {
            var hooker = mainWindow.HandleSource;
            hooker.AddHook(WindowHookCallback);

            logger.Information($"Installed message hook.");

            this.hooker = hooker;
        }

        IntPtr WindowHookCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            logger.Information($"Message received: [{hwnd}, {msg}, {wParam}, {lParam}]");

            foreach (var interceptor in windowMessageInterceptors)
            {
                var argument = new WindowMessageReceivedArgument(hwnd, msg, wParam, lParam);
                interceptor.ReceiveMessageEvent(argument);

                handled |= argument.Handled;

                logger.Information($"Message passed to interceptor {interceptor.GetType().Name}.");
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
