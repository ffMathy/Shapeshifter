using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Windows;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;

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

        public IClipboardDataControlPackage Create(IDataObject data)
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(package, data);
            DecoratePackageWithControl(package);

            return package;
        }

        void DecoratePackageWithClipboardData(ClipboardDataControlPackage package, IDataObject dataObject)
        {
            foreach (var factory in dataFactories)
            {
                foreach (var format in dataObject.GetFormats(true))
                {
                    if (factory.CanBuildData(format))
                    {
                        var rawData = dataObject.GetData(format);

                        var clipboardData = factory.BuildData(format, rawData);
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
