using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System.Windows.Interop;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    interface IClipboardListWindow : IWindow, ISingleInstance
    {
        HwndSource HandleSource { get; }
    }
}
