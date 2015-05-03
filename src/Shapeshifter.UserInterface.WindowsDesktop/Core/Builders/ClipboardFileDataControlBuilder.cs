using System;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;

namespace Shapeshifter.UserInterface.WindowsDesktop.Core.Builders
{
    class ClipboardFileDataControlBuilder : IClipboardDataControlFactory
    {
        public object Build(IClipboardData data)
        {
            return new ClipboardFileDataControl()
            {
                DataContext = new ClipboardFileDataViewModel((ClipboardFileData)data)
            };
        }

        public bool CanBuild(IClipboardData data)
        {
            return data is ClipboardFileData;
        }
    }
}
