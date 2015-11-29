namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Api;

    using Controls.Clipboard.Unwrappers.Interfaces;

    using Data;
    using Data.Interfaces;

    using Handles.Factories.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class ClipboardDataControlPackageFactory: IClipboardDataControlPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;

        readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;

        readonly IEnumerable<IClipboardDataControlFactory> dataFactories;

        readonly IUserInterfaceThread userInterfaceThread;

        public ClipboardDataControlPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories,
            IEnumerable<IMemoryUnwrapper> memoryUnwrappers,
            IClipboardHandleFactory clipboardSessionFactory,
            IUserInterfaceThread userInterfaceThread)
        {
            this.dataFactories = dataFactories;
            this.memoryUnwrappers = memoryUnwrappers;
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
                return IsAnyFormatSupported(formats)
                           ? ConstructPackage(formats)
                           : null;
            }
        }

        IClipboardDataControlPackage ConstructPackage(
            IEnumerable<uint> formats)
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(formats, package);
            userInterfaceThread.Invoke(() => DecoratePackageWithControl(package));

            return package;
        }

        void DecoratePackageWithClipboardData(
            IEnumerable<uint> formats,
            IClipboardDataPackage package)
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
            IClipboardDataPackage package,
            IClipboardDataControlFactory factory,
            uint format)
        {
            var unwrapper = memoryUnwrappers.FirstOrDefault(x => x.CanUnwrap(format));

            var rawData = unwrapper?.UnwrapStructure(format);
            if (rawData == null)
            {
                return;
            }

            var clipboardData = factory.BuildData(format, rawData);
            if (clipboardData != null)
            {
                package.AddData(clipboardData);
            }
        }

        void DecoratePackageWithControl(
            ClipboardDataControlPackage package)
        {
            foreach (var data in package.Contents)
            {
                var dataFactory = dataFactories.FirstOrDefault(x => x.CanBuildControl(data));
                if (dataFactory == null)
                {
                    continue;
                }

                package.Control = dataFactory.BuildControl(data);
                break;
            }
        }
    }
}