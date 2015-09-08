using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;
        readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;
        readonly IEnumerable<IClipboardDataControlFactory> dataFactories;
        readonly IKeyValueCache<uint, byte[]> clipboardCache;
        readonly IUserInterfaceThread userInterfaceThread;

        public ClipboardDataControlPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories,
            IEnumerable<IMemoryUnwrapper> memoryUnwrappers,
            IKeyValueCache<uint, byte[]> clipboardCache,
            IClipboardHandleFactory clipboardSessionFactory,
            IUserInterfaceThread userInterfaceThread)
        {
            this.dataFactories = dataFactories;
            this.memoryUnwrappers = memoryUnwrappers;
            this.clipboardCache = clipboardCache;
            this.clipboardSessionFactory = clipboardSessionFactory;
            this.userInterfaceThread = userInterfaceThread;
        }

        bool IsAnyFormatSupported(
            IEnumerable<uint> formats)
        {
            return dataFactories.Any(
                x => formats.Any(x.CanBuildData));
        }

        public IClipboardDataControlPackage Create()
        {
            using (clipboardSessionFactory.StartNewSession())
            {
                var formats = ClipboardApi.GetClipboardFormats();
                if (IsAnyFormatSupported(formats))
                {
                    return ConstructPackage(formats);
                }
                else
                {
                    return null;
                }
            }
        }

        IClipboardDataControlPackage ConstructPackage(IEnumerable<uint> formats)
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(formats, package);
            userInterfaceThread.Invoke(() => DecoratePackageWithControl(package));

            return package;
        }

        void DecoratePackageWithClipboardData(
            IEnumerable<uint> formats,
            ClipboardDataControlPackage package)
        {
            foreach (var format in formats)
            {
                var dataFactory = dataFactories.FirstOrDefault(x => x.CanBuildData(format));
                if (dataFactory != null)
                {
                    DecoratePackageWithFormatDataUsingFactory(package, dataFactory, format);
                }
            }
        }

        void DecoratePackageWithFormatDataUsingFactory(
            ClipboardDataControlPackage package, IClipboardDataControlFactory factory, uint format)
        {
            var unwrapper = memoryUnwrappers.FirstOrDefault(x => x.CanUnwrap(format));
            if (unwrapper != null)
            {
                var rawData = unwrapper.UnwrapStructure(format);
                if (rawData != null)
                {
                    var clipboardData = factory.BuildData(format, rawData);
                    if (clipboardData != null)
                    {
                        package.AddData(clipboardData);
                    }
                }
            }
        }

        void DecoratePackageWithControl(ClipboardDataControlPackage package)
        {
            foreach (var data in package.Contents)
            {
                var dataFactory = dataFactories.FirstOrDefault(x => x.CanBuildControl(data));
                if (dataFactory != null)
                {
                    package.Control = dataFactory.BuildControl(data);
                    break;
                }
            }
        }
    }
}
