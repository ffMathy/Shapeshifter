using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class BitmapClipboardDataControlFactory : IClipboardDataControlFactory
    {
        private readonly IDataSourceService dataSourceService;

        public BitmapClipboardDataControlFactory(
            IDataSourceService dataSourceService)
        {
            this.dataSourceService = dataSourceService;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
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
            return false;
        }

        public bool CanBuildData(uint format)
        {
            return 
                format == ClipboardApi.CF_BITMAP;
        }
    }
}
