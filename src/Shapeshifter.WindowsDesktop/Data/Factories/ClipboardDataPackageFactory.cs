namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using Data.Interfaces;

    using Infrastructure.Handles.Factories.Interfaces;

    using Interfaces;

    using Structures;

    using Unwrappers.Interfaces;
    using Exceptions;
	using Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces;

	class ClipboardDataPackageFactory: IClipboardDataPackageFactory
    {
        readonly IClipboardHandleFactory clipboardSessionFactory;
		readonly IDataSourceService dataSourceService;
		readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;
        readonly IEnumerable<IClipboardDataFactory> dataFactories;

        public ClipboardDataPackageFactory(
            IEnumerable<IClipboardDataFactory> dataFactories,
            IEnumerable<IMemoryUnwrapper> memoryUnwrappers,
            IClipboardHandleFactory clipboardSessionFactory,
			IDataSourceService dataSourceService)
        {
            this.dataFactories = dataFactories;
            this.memoryUnwrappers = memoryUnwrappers;
            this.clipboardSessionFactory = clipboardSessionFactory;
			this.dataSourceService = dataSourceService;
		}

        bool IsAnyFormatSupported(
            IEnumerable<IClipboardFormat> formats)
        {
            return dataFactories.Any(
                x => formats.Any(x.CanBuildData));
        }

        public IClipboardDataPackage CreateFromCurrentClipboardData()
        {
            using (var session = clipboardSessionFactory.StartNewSession())
            {
                var formats = session.GetClipboardFormats();
                if(formats.Count == 0)
                    throw new NoDataInClipboardException();

                if(!IsAnyFormatSupported(formats))
                    throw new ClipboardFormatNotUnderstoodException();

                return ConstructPackageFromFormats(formats);
            }
        }

        public IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData)
		{
			if (!IsAnyFormatSupported(
				formatsAndData.Select(x => x.Format)))
			{
				return null;
			}

			var package = CreateDataPackage();
			foreach (var pair in formatsAndData)
			{
				DecoratePackageWithClipboardDataFromRawDataAndFormat(package, pair.Format, pair.Data);
			}

			return package;
		}

		ClipboardDataPackage CreateDataPackage()
		{
			return new ClipboardDataPackage() {
				Source = dataSourceService.GetDataSource()
			};
		}

		IClipboardDataPackage ConstructPackageFromFormats(
            IEnumerable<IClipboardFormat> formats)
        {
            var package = CreateDataPackage();
            DecoratePackageWithClipboardData(formats, package);

            return package;
        }

        void DecoratePackageWithClipboardData(
            IEnumerable<IClipboardFormat> formats,
            IClipboardDataPackage package)
        {
            foreach (var format in formats)
                DecoratePackageWithClipboardDataForFormat(package, format);
        }

        IClipboardDataFactory FindCapableFactoryFromFormat(IClipboardFormat format)
        {
            return dataFactories.FirstOrDefault(x => x.CanBuildData(format));
        }

        void DecoratePackageWithClipboardDataForFormat(
            IClipboardDataPackage package,
			IClipboardFormat format)
        {
            var unwrapper = memoryUnwrappers
                .FirstOrDefault(
                    x => x.CanUnwrap(format));

            var rawData = unwrapper?.UnwrapStructure(format);
            if (rawData == null)
                return;

            DecoratePackageWithClipboardDataFromRawDataAndFormat(package, format, rawData);
        }

        void DecoratePackageWithClipboardDataFromRawDataAndFormat(
            IClipboardDataPackage package,
			IClipboardFormat format,
            byte[] rawData)
        {
            var factory = FindCapableFactoryFromFormat(format);

            var clipboardData = factory?.BuildData(format, rawData);
            if (clipboardData != null)
                package.AddData(clipboardData);
        }
    }
}