using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;
        readonly IEnumerable<IClipboardDataControlFactory> dataFactories;

        public ClipboardDataControlPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories,
            IClipboardHandleFactory clipboardSessionFactory)
        {
            this.dataFactories = dataFactories;
            this.clipboardSessionFactory = clipboardSessionFactory;
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
            using (clipboardSessionFactory.StartNewSession())
            {
                foreach (var factory in dataFactories)
                {
                    foreach (var format in ClipboardApi.GetClipboardFormats())
                    {
                        DecoratePackageWithFormatDataUsingFactory(package, factory, format);
                    }
                }
            }
        }

        static void DecoratePackageWithFormatDataUsingFactory(ClipboardDataControlPackage package, IClipboardDataControlFactory factory, uint format)
        {
            if (factory.CanBuildData(format))
            {
                var rawData = ClipboardApi.GetClipboardDataBytes(format);

                var clipboardData = factory.BuildData(format, rawData);
                package.AddData(clipboardData);
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
