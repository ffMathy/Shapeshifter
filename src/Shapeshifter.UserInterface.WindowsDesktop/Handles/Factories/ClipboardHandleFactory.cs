using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class ClipboardHandleFactory : IClipboardHandleFactory
    {
        readonly IWindowMessageHook windowMessageHook;

        public ClipboardHandleFactory(
            IWindowMessageHook windowMessageHook)
        {
            this.windowMessageHook = windowMessageHook;
        }

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(windowMessageHook);
        }
    }
}
