using System.Collections.Generic;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardDataControlPackage
    {
        void AddData(IClipboardData data);

        IEnumerable<IClipboardData> Contents { get; }

        IClipboardControl Control { get; }
    }
}
