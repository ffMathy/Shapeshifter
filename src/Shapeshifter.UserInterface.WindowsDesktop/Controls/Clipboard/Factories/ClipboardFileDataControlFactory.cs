namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardFileDataControlFactory
        : IClipboardDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

        public ClipboardFileDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardFileData;
        }

        public IClipboardControl BuildControl(IClipboardData data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

            return CreateFileDataControl((IClipboardFileData)data);
        }

        
        IClipboardControl CreateFileDataControl(IClipboardFileData data)
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