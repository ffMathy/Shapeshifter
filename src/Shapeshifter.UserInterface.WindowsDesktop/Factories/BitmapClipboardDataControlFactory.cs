using System;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class BitmapClipboardDataControlFactory : IClipboardDataControlFactory
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
            return new ClipboardImageDataControl()
            {
                DataContext = new ClipboardImageDataViewModel(environmentInformation)
                {
                    Data = (IClipboardImageData)clipboardData
                }
            };
        }

        public IClipboardData BuildData(uint format, byte[] rawData)
        {
            if(!CanBuildData(format))
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
