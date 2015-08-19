using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    class ClipboardFileCollectionDataControlFactory
        : IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
    {
        public IClipboardFileCollectionDataControl CreateControl(IClipboardFileCollectionData data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data must be set when constructing a clipboard control.", nameof(data));
            }

            return CreateClipboardFileCollectionDataControl(data);
        }

        [ExcludeFromCodeCoverage]
        private static IClipboardFileCollectionDataControl CreateClipboardFileCollectionDataControl(IClipboardFileCollectionData data)
        {
            return new ClipboardFileCollectionDataControl()
            {
                DataContext = new ClipboardFileCollectionDataViewModel()
                {
                    Data = data
                }
            };
        }
    }
}
