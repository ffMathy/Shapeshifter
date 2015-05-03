using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardHookService : IClipboardHookService
    {

        public event EventHandler<DataCopiedEventArgument> DataCopied;

        private uint lastClipboardItemIdentifier;

        private readonly IEnumerable<IClipboardDataFactory> dataFactories;

        public ClipboardHookService(IEnumerable<IClipboardDataFactory> dataFactories)
        {
            this.dataFactories = dataFactories;
        }

        public bool IsConnected
        {
            get; private set;
        }

        public void Connect()
        {
            if (!IsConnected)
            {
                lock (this)
                {
                    var mainWindow = App.Current.MainWindow;
                    if (mainWindow == null)
                    {
                        throw new InvalidOperationException("Can't install a clipboard hook when there is no window open.");
                    }

                    var interopHelper = new WindowInteropHelper(mainWindow);
                    var windowHandle = interopHelper.EnsureHandle();

                    var hooker = PresentationSource.FromVisual(mainWindow) as HwndSource;
                    if (hooker == null)
                    {
                        throw new InvalidOperationException("Could not fetch the handle of the main window.");
                    }

                    //now try installing a clipboard hook.
                    if (!ClipboardApi.AddClipboardFormatListener(windowHandle))
                    {
                        throw new NotImplementedException("Could not install a clipboard hook for the main window.");
                    }

                    hooker.AddHook(WindowHookCallback);

                    IsConnected = true;
                }
            }
        }

        private IntPtr WindowHookCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == ClipboardApi.WM_CLIPBOARDUPDATE)
            {

                var clipboardItemIdentifier = ClipboardApi.GetClipboardSequenceNumber();
                if (clipboardItemIdentifier != lastClipboardItemIdentifier)
                {
                    lastClipboardItemIdentifier = clipboardItemIdentifier;

                    HandleCopiedClipboardData();
                }
            }

            return IntPtr.Zero;
        }

        private void HandleCopiedClipboardData()
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject != null)
            {
                foreach (var factory in dataFactories)
                {
                    foreach (var format in dataObject.GetFormats())
                    {
                        if (factory.CanBuild(format))
                        {
                            throw new NotImplementedException();

                            break;
                        }
                    }
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
