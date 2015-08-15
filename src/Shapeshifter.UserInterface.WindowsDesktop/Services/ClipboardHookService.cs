using System;
using System.Windows;
using System.Windows.Interop;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using WindowsClipboard = System.Windows.Clipboard;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class ClipboardHookService : IClipboardHookService
    {

        public event EventHandler<DataCopiedEventArgument> DataCopied;

        private uint lastClipboardItemIdentifier;

        public bool IsConnected
        {
            get; private set;
        }

        public void Connect()
        {
            EnsureWindowIsPresent();

            if (!IsConnected)
            {
                InstallHooks();
                IsConnected = true;
            }
        }

        private void InstallHooks()
        {
            InstallClipboardHook();
            InstallWindowMessageHook();
        }

        private static void EnsureWindowIsPresent()
        {
            var mainWindow = App.Current.MainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Can't install a clipboard hook when there is no window open.");
            }
        }

        private void InstallWindowMessageHook()
        {
            var hooker = FetchHandleSource();
            hooker.AddHook(WindowHookCallback);
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

        private static void InstallClipboardHook()
        {
            var windowHandle = FetchWindowHandle(App.Current.MainWindow);
            if (!ClipboardApi.AddClipboardFormatListener(windowHandle))
            {
                throw new NotImplementedException("Could not install a clipboard hook for the main window.");
            }
        }

        private static IntPtr FetchWindowHandle(Window mainWindow)
        {
            var interopHelper = new WindowInteropHelper(mainWindow);
            var windowHandle = interopHelper.EnsureHandle();
            return windowHandle;
        }

        private IntPtr WindowHookCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == ClipboardApi.WM_CLIPBOARDUPDATE)
            {
                HandleClipboardUpdateWindowMessage();
            }

            return IntPtr.Zero;
        }

        private void HandleClipboardUpdateWindowMessage()
        {
            var clipboardItemIdentifier = ClipboardApi.GetClipboardSequenceNumber();
            if (clipboardItemIdentifier != lastClipboardItemIdentifier)
            {
                lastClipboardItemIdentifier = clipboardItemIdentifier;

                TriggerDataCopiedEvent();
            }
        }

        private void TriggerDataCopiedEvent()
        {
            if (DataCopied != null)
            {
                var data = WindowsClipboard.GetDataObject();
                if (data != null)
                {
                    DataCopied(this, new DataCopiedEventArgument(data));
                }
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                IsConnected = false;
            }
        }
    }
}
