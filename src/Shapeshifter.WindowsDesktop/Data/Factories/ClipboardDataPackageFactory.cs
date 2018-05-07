﻿namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	using System.Collections.Generic;
	using System.Linq;

	using Data.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;

	using Interfaces;

	using Unwrappers.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces;
	using Serilog;

	class ClipboardDataPackageFactory : IClipboardDataPackageFactory
	{
		readonly IClipboardHandleFactory clipboardSessionFactory;
		readonly IDataSourceService dataSourceService;
		readonly IGeneralUnwrapper generalUnwrapper;
		readonly ILogger logger;
		readonly ICustomClipboardDataFactory customClipboardDataFactory;

		readonly IEnumerable<IMemoryUnwrapper> allMemoryUnwrappers;
		readonly IEnumerable<IClipboardDataFactory> allDataFactories;

		public ClipboardDataPackageFactory(
			IEnumerable<IClipboardDataFactory> allDataFactories,
			IEnumerable<IMemoryUnwrapper> allMemoryUnwrappers,
			ICustomClipboardDataFactory customClipboardDataFactory,
			IGeneralUnwrapper generalUnwrapper,
			ILogger logger,
			IClipboardHandleFactory clipboardSessionFactory,
			IDataSourceService dataSourceService)
		{
			this.allDataFactories = allDataFactories;
			this.allMemoryUnwrappers = allMemoryUnwrappers;
			this.customClipboardDataFactory = customClipboardDataFactory;
			this.generalUnwrapper = generalUnwrapper;
			this.logger = logger;
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
			if(unwrapper == null)
				return;

			logger.Information("Unwrapping format {format} using {unwrapper}.", format, unwrapper.GetType().Name);

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
			if (unwrapper == null && generalUnwrapper.CanUnwrap(format))
				unwrapper = generalUnwrapper;

			return unwrapper;
		}

		void DecoratePackageWithClipboardDataFromRawDataAndFormat(
			IClipboardDataPackage package,
			IClipboardFormat format,
			byte[] rawData)
		{
			var factory = FindCapableFactoryFromFormat(format);
			logger.Information("Generating clipboard data object for format {format} using {factory}.", format, factory.GetType().Name);

			var clipboardData = factory.BuildData(format, rawData);
			if (clipboardData != null)
				package.AddData(clipboardData);
		}
	}
}