using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardFileCollectionDataControlFactory
        : IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
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
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateClipboardFileCollectionDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        IClipboardFileCollectionDataControl CreateClipboardFileCollectionDataControl(IClipboardFileCollectionData data)
        {
            return new ClipboardFileCollectionDataControl()
            {
                DataContext = new ClipboardFileCollectionDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}
