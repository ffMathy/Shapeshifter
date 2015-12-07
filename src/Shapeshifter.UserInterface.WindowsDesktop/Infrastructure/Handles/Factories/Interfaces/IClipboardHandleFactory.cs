namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Handles.Factories.Interfaces
{
    using Handles.Interfaces;

    public interface IClipboardHandleFactory
    {
        IClipboardHandle StartNewSession();
    }
}