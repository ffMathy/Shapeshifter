namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels.FileCollection;

    class ClipboardFileCollectionDataControlFactory
        : IClipboardFileCollectionDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardFileCollectionDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardFileCollectionData;
        }

        public IClipboardControl BuildControl(
            IClipboardData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateClipboardFileCollectionDataControl((IClipboardFileCollectionData)data);
        }

        
        IClipboardControl CreateClipboardFileCollectionDataControl(
            IClipboardFileCollectionData data)
        {
            return new ClipboardFileCollectionDataControl
            {
                DataContext = new ClipboardFileCollectionDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}