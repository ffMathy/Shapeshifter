namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardTextDataControlFactory:
        IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardTextDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardTextDataControl CreateControl(IClipboardTextData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateClipboardTextDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        IClipboardTextDataControl CreateClipboardTextDataControl(IClipboardTextData data)
        {
            return new ClipboardTextDataControl
            {
                DataContext = new ClipboardTextDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}