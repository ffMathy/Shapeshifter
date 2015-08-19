using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardFileDataControlFactory
        : IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
    {
        public IClipboardFileDataControl CreateControl(IClipboardFileData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateFileDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private static IClipboardFileDataControl CreateFileDataControl(IClipboardFileData data)
        {
            return new ClipboardFileDataControl()
            {
                DataContext = new ClipboardFileDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
