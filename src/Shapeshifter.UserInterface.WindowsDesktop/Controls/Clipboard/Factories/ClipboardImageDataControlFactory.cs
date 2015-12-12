namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardImageDataControlFactory
        : IClipboardDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardImageDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardImageData;
        }

        public IClipboardControl BuildControl(IClipboardData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateImageDataControl((IClipboardImageData)data);
        }

        
        IClipboardControl CreateImageDataControl(IClipboardImageData data)
        {
            return new ClipboardImageDataControl
            {
                DataContext = new ClipboardImageDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}