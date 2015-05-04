using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class ClipboardManagementService : IClipboardManagementService
    {

        private readonly IEnumerable<IClipboardDataControlFactory> dataFactories;
        private readonly IList<IClipboardControlDataPackage> clipboardPackages;

        public ClipboardManagementService(IEnumerable<IClipboardDataControlFactory> dataFactories, IClipboardHookService clipboardHook, IKeyboardHookService keyboardHook)
        {
            this.dataFactories = dataFactories;

            this.clipboardPackages = new List<IClipboardControlDataPackage>();

            //listen to hook events.
            clipboardHook.DataCopied += ClipboardHook_DataCopied;
        }

        private void ClipboardHook_DataCopied(object sender, Events.DataCopiedEventArgument e)
        {
            var dataObject = e.Data;

            var package = new ClipboardDataControlPackage();
            foreach (var factory in dataFactories)
            {
                foreach (var format in dataObject.GetFormats())
                {
                    if (factory.CanBuildData(format))
                    {
                        var rawData = dataObject.GetData(format);

                        var clipboardData = factory.BuildData(format, rawData);
                        package.AddData(clipboardData);
                    }
                }
            }

            //construct a control if the package contains any data.
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

        public IEnumerable<IClipboardControlDataPackage> ClipboardElements
        {
            get
            {
                return clipboardPackages;
            }
        }
    }
}
