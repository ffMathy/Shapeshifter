namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	using System.Collections.Generic;
	using System.Linq;

	using Data.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;

	using Interfaces;

	using Structures;

	using Unwrappers.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces;

	class ClipboardDataPackageFactory : IClipboardDataPackageFactory
	{
		readonly IClipboardHandleFactory clipboardSessionFactory;
		readonly IDataSourceService dataSourceService;
		readonly IGeneralUnwrapper generalUnwrapper;
		readonly ICustomClipboardDataFactory customClipboardDataFactory;

		readonly IEnumerable<IMemoryUnwrapper> allMemoryUnwrappers;
		readonly IEnumerable<IClipboardDataFactory> allDataFactories;

		public ClipboardDataPackageFactory(
			IEnumerable<IClipboardDataFactory> allDataFactories,
			IEnumerable<IMemoryUnwrapper> allMemoryUnwrappers,
			ICustomClipboardDataFactory customClipboardDataFactory,
			IGeneralUnwrapper generalUnwrapper,
			IClipboardHandleFactory clipboardSessionFactory,
			IDataSourceService dataSourceService)
		{
			this.allDataFactories = allDataFactories;
			this.allMemoryUnwrappers = allMemoryUnwrappers;
			this.customClipboardDataFactory = customClipboardDataFactory;
			this.generalUnwrapper = generalUnwrapper;
			this.clipboardSessionFactory = clipboardSessionFactory;
			this.dataSourceService = dataSourceService;
		}

		public IClipboardDataPackage CreateFromCurrentClipboardData()
		{
			using (var session = clipboardSessionFactory.StartNewSession())
			{
				var formats = session.GetClipboardFormats();
				if (formats.Count == 0)
					return null;

				return ConstructPackageFromFormats(formats);
			}
		}

		public IClipboardDataPackage CreateFromFormatsAndData(params FormatDataPair[] formatsAndData)
		{
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
			var factory = allDataFactories
				.Where(x => x.GetType() != customClipboardDataFactory.GetType())
				.FirstOrDefault(x => x.CanBuildData(format));
			if(factory == null)
				factory = customClipboardDataFactory;

			return factory;
		}

		void DecoratePackageWithClipboardDataForFormat(
			IClipboardDataPackage package,
			IClipboardFormat format)
		{
			var unwrapper = FindCapableUnwrapperFromFormat(format);
			var rawData = unwrapper.UnwrapStructure(format);
			if (rawData == null)
				return;

			DecoratePackageWithClipboardDataFromRawDataAndFormat(package, format, rawData);
		}

		IMemoryUnwrapper FindCapableUnwrapperFromFormat(IClipboardFormat format)
		{
			var unwrapper = allMemoryUnwrappers
				.Where(x => x.GetType() != generalUnwrapper.GetType())
				.FirstOrDefault(x => x.CanUnwrap(format));
			if (unwrapper == null)
				unwrapper = generalUnwrapper;

			return unwrapper;
		}

		void DecoratePackageWithClipboardDataFromRawDataAndFormat(
			IClipboardDataPackage package,
			IClipboardFormat format,
			byte[] rawData)
		{
			var factory = FindCapableFactoryFromFormat(format);
			var clipboardData = factory.BuildData(format, rawData);
			if (clipboardData != null)
				package.AddData(clipboardData);
		}
	}
}