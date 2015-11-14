using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    public interface IClipboardDataControlFactory
    {
        bool CanBuildData(uint format);

        bool CanBuildControl(IClipboardData data);

        IClipboardControl BuildControl(IClipboardData clipboardData);

        IClipboardData BuildData(uint format, byte[] rawData);
    }
}