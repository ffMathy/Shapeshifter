using System.Collections.Generic;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    using Api;

    using Controls.Clipboard.Unwrappers.Interfaces;

    using Data;
    using Data.Interfaces;

    using Handles.Factories.Interfaces;

    using Interfaces;

    class ClipboardDataPackageFactory : IClipboardDataPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;

        readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;

        readonly IEnumerable<IClipboardDataControlFactory> dataFactories;

        public ClipboardDataPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories,
            IEnumerable<IMemoryUnwrapper> memoryUnwrappers,
            IClipboardHandleFactory clipboardSessionFactory)
        {
            this.dataFactories = dataFactories;
            this.memoryUnwrappers = memoryUnwrappers;
            this.clipboardSessionFactory = clipboardSessionFactory;
        }

        bool IsAnyFormatSupported(
            IEnumerable<uint> formats)
        {
            return dataFactories.Any(
                x => formats.Any(x.CanBuildData));
        }

        public IClipboardDataPackage CreateFromCurrentClipboardData()
        {
            using (clipboardSessionFactory.StartNewSession())
            {
                var formats = ClipboardApi.GetClipboardFormats();
                return IsAnyFormatSupported(formats)
                           ? ConstructPackageFromFormats(formats)
                           : null;
            }
        }

        IClipboardDataControlPackage ConstructPackageFromFormats(
            IEnumerable<uint> formats)
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(formats, package);

            return package;
        }

        void DecoratePackageWithClipboardData(
            IEnumerable<uint> formats,
            IClipboardDataPackage package)
        {
            foreach (var format in formats)
            {
                DecoratePackageWithClipboardDataForFormat(package, format);
            }
        }

        IClipboardDataControlFactory FindCapableFactoryFromFormat(uint format)
        {
            return dataFactories.FirstOrDefault(x => x.CanBuildData(format));
        }

        void DecoratePackageWithClipboardDataForFormat(
            IClipboardDataPackage package,
            uint format)
        {
            var unwrapper = memoryUnwrappers.FirstOrDefault(x => x.CanUnwrap(format));

            var rawData = unwrapper?.UnwrapStructure(format);
            if (rawData == null)
            {
                return;
            }

            DecoratePackageWithRawDataUsingFactory(package, format, rawData);
        }

        void DecoratePackageWithRawDataUsingFactory(
            IClipboardDataPackage package,
            uint format,
            byte[] rawData)
        {
            var factory = FindCapableFactoryFromFormat(format);
            var clipboardData = factory.BuildData(format, rawData);
            if (clipboardData != null)
            {
                package.AddData(clipboardData);
            }
        }
    }
}
