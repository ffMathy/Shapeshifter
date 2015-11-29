namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardFileDataControlFactory
        : IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardFileDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardFileDataControl CreateControl(IClipboardFileData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateFileDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        IClipboardFileDataControl CreateFileDataControl(IClipboardFileData data)
        {
            return new ClipboardFileDataControl
            {
                DataContext = new ClipboardFileDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}