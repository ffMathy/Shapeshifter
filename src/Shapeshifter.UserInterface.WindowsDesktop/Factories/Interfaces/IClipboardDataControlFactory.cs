using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    public interface IClipboardDataControlFactory
    {
        bool CanBuildData(string format);

        bool CanBuildControl(IClipboardData data);

        IClipboardControl BuildControl(IClipboardData clipboardData);

        IClipboardData BuildData(string format, object data);
    }
}
