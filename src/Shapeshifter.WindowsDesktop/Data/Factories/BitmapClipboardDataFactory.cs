namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;

    using Api;

    using Data;
    using Data.Interfaces;

    using Interfaces;

    using Services.Interfaces;

    class BitmapClipboardDataFactory : IBitmapClipboardDataFactory
    {
        readonly IDataSourceService dataSourceService;

        public BitmapClipboardDataFactory(
            IDataSourceService dataSourceService)
        {
            this.dataSourceService = dataSourceService;
        }

        public IClipboardData BuildData(uint format, byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new InvalidOperationException("The given format is not supported.");
            }

            return new ClipboardImageData(dataSourceService)
            {
                RawData = rawData,
                RawFormat = format
            };
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardApi.CF_BITMAP;
        }
    }
}
