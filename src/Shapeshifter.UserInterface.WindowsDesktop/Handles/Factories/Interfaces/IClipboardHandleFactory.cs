using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IClipboardHandleFactory
    {
        IClipboardHandle StartNewSession();
    }
}