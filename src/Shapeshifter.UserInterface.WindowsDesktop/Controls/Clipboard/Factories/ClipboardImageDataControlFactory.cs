namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardImageDataControlFactory
        : IClipboardControlFactory<IClipboardImageData, IClipboardImageDataControl>
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardImageDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardImageDataControl CreateControl(IClipboardImageData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateImageDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        IClipboardImageDataControl CreateImageDataControl(IClipboardImageData data)
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