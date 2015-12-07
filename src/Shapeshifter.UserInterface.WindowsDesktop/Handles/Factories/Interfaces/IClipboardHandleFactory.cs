namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    using Handles.Interfaces;

    public interface IClipboardHandleFactory
    {
        IClipboardHandle StartNewSession();
    }
}