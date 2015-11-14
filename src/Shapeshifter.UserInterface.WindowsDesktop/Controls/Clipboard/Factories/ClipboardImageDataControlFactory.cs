#region

using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    internal class ClipboardImageDataControlFactory
        : IClipboardControlFactory<IClipboardImageData, IClipboardImageDataControl>
    {
        private readonly IEnvironmentInformation environmentInformation;

        public ClipboardImageDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardImageDataControl CreateControl(IClipboardImageData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateImageDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private IClipboardImageDataControl CreateImageDataControl(IClipboardImageData data)
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