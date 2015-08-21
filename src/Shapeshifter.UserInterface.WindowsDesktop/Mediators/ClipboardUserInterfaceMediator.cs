using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardUserInterfaceMediator : 
        IClipboardUserInterfaceMediator
    {
        private readonly IClipboardHookService clipboardHook;
        private readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        private readonly IEnumerable<IClipboardDataControlFactory> dataFactories;
        private readonly IList<IClipboardControlDataPackage> clipboardPackages;

        public event EventHandler<ControlEventArgument> ControlAdded;
        public event EventHandler<ControlEventArgument> ControlRemoved;
        public event EventHandler<ControlEventArgument> ControlPinned;
        public event EventHandler<ControlEventArgument> ControlHighlighted;

        public bool IsConnected
        {
            get
            {
                return clipboardHook.IsConnected && pasteHotkeyInterceptor.IsConnected;
            }
        }

        public ClipboardUserInterfaceMediator(
            IEnumerable<IClipboardDataControlFactory> dataFactories, 
            IClipboardHookService clipboardHook,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor)
        {
            this.dataFactories = dataFactories;

            clipboardPackages = new List<IClipboardControlDataPackage>();
            
            this.clipboardHook = clipboardHook;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
        }

        private void ClipboardHook_DataCopied(object sender, DataCopiedEventArgument e)
        {
            var dataObject = e.Data;

            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(dataObject, package);
            DecoratePackageWithControl(package);

            clipboardPackages.Add(package);

            //signal an added event.
            if (ControlAdded != null)
            {
                ControlAdded(this, new ControlEventArgument(package));
            }
        }

        private void DecoratePackageWithClipboardData(System.Windows.IDataObject dataObject, ClipboardDataControlPackage package)
        {
            foreach (var factory in dataFactories)
            {
                foreach (var format in dataObject.GetFormats(true))
                {
                    if (factory.CanBuildData(format))
                    {
                        //TODO: add a retrying circuit breaker
                        var rawData = dataObject.GetData(format);

                        var clipboardData = factory.BuildData(format, rawData);
                        package.AddData(clipboardData);
                    }
                }
            }
        }

        private void DecoratePackageWithControl(ClipboardDataControlPackage package)
        {
            foreach (var factory in dataFactories)
            {
                foreach (var data in package.Contents)
                {
                    if (factory.CanBuildControl(data))
                    {
                        package.Control = factory.BuildControl(data);
                        break;
                    }
                }
            }
        }

        public void Disconnect()
        {
            UninstallClipboardHook();
            UninstallPasteHotkeyInterceptor();
        }

        private void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteHotkeyFired -= PasteHotkeyInterceptor_PasteHotkeyFired;
            pasteHotkeyInterceptor.Disconnect();
        }

        private void UninstallClipboardHook()
        {
            clipboardHook.DataCopied -= ClipboardHook_DataCopied;
            clipboardHook.Disconnect();
        }

        public void Connect()
        {
            InstallClipboardHook();
            InstallPasteHotkeyInterceptor();
        }

        private void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteHotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
            pasteHotkeyInterceptor.Connect();
        }

        private void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, PasteHotkeyFiredArgument e)
        {
            throw new NotImplementedException();
        }

        private void InstallClipboardHook()
        {
            clipboardHook.Connect();
            clipboardHook.DataCopied += ClipboardHook_DataCopied;
        }

        public IEnumerable<IClipboardControlDataPackage> ClipboardElements
        {
            get
            {
                return clipboardPackages;
            }
        }
    }
}
