using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Windows;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
    {
        readonly IEnumerable<IClipboardDataControlFactory> dataFactories;

        public ClipboardDataControlPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories)
        {
            this.dataFactories = dataFactories;
        }

        public IClipboardDataControlPackage Create()
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(package);
            DecoratePackageWithControl(package);

            return package;
        }

        void DecoratePackageWithClipboardData(ClipboardDataControlPackage package)
        {
            foreach (var factory in dataFactories)
            {
                foreach (var format in ClipboardApi.GetClipboardFormats())
                {
                    var formatName = ClipboardApi.GetClipboardFormatName(format);
                    if (factory.CanBuildData(formatName))
                    {
                        var rawData = ClipboardApi.GetClipboardDataBytes(format);

                        var clipboardData = factory.BuildData(formatName, rawData);
                        package.AddData(clipboardData);
                    }
                }
            }
        }

        void DecoratePackageWithControl(ClipboardDataControlPackage package)
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
    }
}
