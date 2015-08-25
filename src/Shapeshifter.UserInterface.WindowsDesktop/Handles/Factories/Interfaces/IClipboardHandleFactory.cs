using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    public interface IClipboardHandleFactory
    {
        IClipboardHandle StartNewSession();
    }
}
