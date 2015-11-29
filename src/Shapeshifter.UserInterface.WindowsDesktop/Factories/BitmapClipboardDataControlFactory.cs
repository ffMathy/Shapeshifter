namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    using System;

    using Api;

    using Controls.Clipboard;
    using Controls.Clipboard.Interfaces;
    using Controls.Clipboard.ViewModels;

    using Data;
    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using Services.Interfaces;

    class BitmapClipboardDataControlFactory: IClipboardDataControlFactory
    {
        readonly IDataSourceService dataSourceService;

        readonly IEnvironmentInformation environmentInformation;

        public BitmapClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IEnvironmentInformation environmentInformation)
        {
            this.dataSourceService = dataSourceService;
            this.environmentInformation = environmentInformation;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            return new ClipboardImageDataControl
            {
                DataContext = new ClipboardImageDataViewModel(environmentInformation)
                {
                    Data = (IClipboardImageData) clipboardData
                }
            };
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

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardImageData;
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardApi.CF_BITMAP;
        }
    }
}