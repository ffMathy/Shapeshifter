#region

using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IClipboardHandleFactory
    {
        IClipboardHandle StartNewSession();
    }
}