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
    internal class ClipboardTextDataControlFactory :
        IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>
    {
        private readonly IEnvironmentInformation environmentInformation;

        public ClipboardTextDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public IClipboardTextDataControl CreateControl(IClipboardTextData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateClipboardTextDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private IClipboardTextDataControl CreateClipboardTextDataControl(IClipboardTextData data)
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