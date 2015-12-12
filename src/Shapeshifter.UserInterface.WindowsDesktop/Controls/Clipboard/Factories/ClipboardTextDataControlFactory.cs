namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardTextDataControlFactory :
        IClipboardDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardTextDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardTextData;
        }

        public IClipboardControl BuildControl(IClipboardData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateClipboardTextDataControl((IClipboardTextData)data);
        }

        
        IClipboardControl CreateClipboardTextDataControl(IClipboardTextData data)
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