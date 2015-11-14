#region

using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
    public interface IClipboardControlFactory<TData, TControl>
        where TData : IClipboardData
        where TControl : IClipboardControl
    {
        TControl CreateControl(TData data);
    }
}