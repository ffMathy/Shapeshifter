using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    interface IClipboardDataControlFactory
    {
        bool CanBuildData(string format);

        bool CanBuildControl(IClipboardData data);

        UIElement BuildControl(IClipboardData clipboardData);

        IClipboardData BuildData(string format, object data);
    }
}
