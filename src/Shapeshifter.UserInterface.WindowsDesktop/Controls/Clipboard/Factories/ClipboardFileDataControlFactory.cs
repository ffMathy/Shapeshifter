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
    internal class ClipboardFileDataControlFactory
        : IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
    {
        private readonly IEnvironmentInformation environmentInformation;

        public ClipboardFileDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardFileDataControl CreateControl(IClipboardFileData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateFileDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private IClipboardFileDataControl CreateFileDataControl(IClipboardFileData data)
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