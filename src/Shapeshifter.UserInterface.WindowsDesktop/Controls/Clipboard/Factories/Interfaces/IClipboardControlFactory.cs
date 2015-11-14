using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    public interface IClipboardControlFactory<in TData, out TControl>
        where TData : IClipboardData
        where TControl : IClipboardControl
    {
        TControl CreateControl(TData data);
    }
}