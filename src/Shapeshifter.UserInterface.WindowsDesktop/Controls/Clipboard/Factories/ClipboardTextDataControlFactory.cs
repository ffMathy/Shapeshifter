using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardTextDataControlFactory : IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>
    {
        public IClipboardTextDataControl CreateControl(IClipboardTextData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }
            
            return CreateClipboardTextDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private static IClipboardTextDataControl CreateClipboardTextDataControl(IClipboardTextData data)
        {
            return new ClipboardTextDataControl()
            {
                DataContext = new ClipboardTextDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
