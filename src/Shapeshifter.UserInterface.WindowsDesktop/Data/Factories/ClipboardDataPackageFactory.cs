namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Factories
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Api;

    using Controls.Clipboard.Unwrappers.Interfaces;

    using Data;
    using Data.Interfaces;

    using Handles.Factories.Interfaces;

    using Interfaces;

    using Structures;

    [ExcludeFromCodeCoverage]
    class ClipboardDataPackageFactory : IClipboardDataPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;

        readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;

        readonly IEnumerable<IClipboardDataFactory> dataFactories;

        public ClipboardDataPackageFactory(
            IEnumerable<IClipboardDataFactory> dataFactories,
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

        public IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData)
        {
            if (!IsAnyFormatSupported(formatsAndData.Select(x => x.Format)))
            {
                return null;
            }

            var package = new ClipboardDataPackage();
            foreach (var pair in formatsAndData)
            {
                DecoratePackageWithClipboardDataFromRawDataAndFormat(package, pair.Format, pair.Data);
            }

            return package;
        }

        IClipboardDataPackage ConstructPackageFromFormats(
            IEnumerable<uint> formats)
        {
            var package = new ClipboardDataPackage();
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

        IClipboardDataFactory FindCapableFactoryFromFormat(uint format)
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

            DecoratePackageWithClipboardDataFromRawDataAndFormat(package, format, rawData);
        }

        void DecoratePackageWithClipboardDataFromRawDataAndFormat(
            IClipboardDataPackage package,
            uint format,
            byte[] rawData)
        {
            var factory = FindCapableFactoryFromFormat(format);
            if (factory == null) return;

            var clipboardData = factory.BuildData(format, rawData);
            if (clipboardData != null)
            {
                package.AddData(clipboardData);
            }
        }
    }
}
