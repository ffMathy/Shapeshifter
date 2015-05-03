using System;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard;

namespace Shapeshifter.UserInterface.WindowsDesktop.Core.Builders
{
    class ClipboardFileDataControlBuilder : IClipboardDataControlFactory<ClipboardFileDataControl>
    {
        public ClipboardFileDataControl Build(IClipboardData data)
        {
            return new ClipboardFileDataControl()
            {
                DataContext = data
            };
        }

        public bool CanBuild(IClipboardData data)
        {
            return data is ClipboardFileData;
        }
    }
}
