namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels.FileCollection;

    class ClipboardFileCollectionDataControlFactory
        : IClipboardControlFactory
              <IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardFileCollectionDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardFileCollectionDataControl CreateControl(IClipboardFileCollectionData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateClipboardFileCollectionDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        IClipboardFileCollectionDataControl CreateClipboardFileCollectionDataControl(
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