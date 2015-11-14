#region

using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    internal class ClipboardFileCollectionDataControlFactory
        : IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
    {
        private readonly IEnvironmentInformation environmentInformation;

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
        private IClipboardFileCollectionDataControl CreateClipboardFileCollectionDataControl(
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