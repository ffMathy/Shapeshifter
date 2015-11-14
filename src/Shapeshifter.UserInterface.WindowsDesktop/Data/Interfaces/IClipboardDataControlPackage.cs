#region

using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardDataControlPackage : IClipboardDataPackage
    {
        IClipboardControl Control { get; }
    }
}