using System.Collections.Generic;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    interface IClipboardControlDataPackage
    {
        void AddData(IClipboardData data);

        IEnumerable<IClipboardData> Contents { get; }

        IClipboardControl Control { get; }
    }
}
