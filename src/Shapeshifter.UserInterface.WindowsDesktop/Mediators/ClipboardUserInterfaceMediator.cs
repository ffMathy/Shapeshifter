using System;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardUserInterfaceMediator : 
        IClipboardUserInterfaceMediator
    {
        private readonly IClipboardHookService clipboardHook;
        private readonly IKeyboardHookService keyboardHook;

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
                return clipboardHook.IsConnected && keyboardHook.IsConnected;
            }
        }

        public ClipboardUserInterfaceMediator(
            IEnumerable<IClipboardDataControlFactory> dataFactories, 
            IClipboardHookService clipboardHook, 
            IKeyboardHookService keyboardHook)
        {
            this.dataFactories = dataFactories;

            clipboardPackages = new List<IClipboardControlDataPackage>();
            
            this.clipboardHook = clipboardHook;
            this.keyboardHook = keyboardHook;
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
            clipboardHook.DataCopied -= ClipboardHook_DataCopied;

            keyboardHook.Disconnect();
            clipboardHook.Disconnect();
        }

        public void Connect()
        {
            keyboardHook.Connect();
            clipboardHook.Connect();

            clipboardHook.DataCopied += ClipboardHook_DataCopied;

            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.KeyUp += KeyboardHook_KeyUp;
        }

        private void KeyboardHook_KeyUp(object sender, KeyEventArgument e, ref bool blockKeystroke)
        {
            throw new NotImplementedException();
        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgument e, ref bool blockKeystroke)
        {
            throw new NotImplementedException();
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
