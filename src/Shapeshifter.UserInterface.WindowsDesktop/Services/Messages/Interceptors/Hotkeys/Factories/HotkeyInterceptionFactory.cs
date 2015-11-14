using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories
{
    class HotkeyInterceptionFactory : IHotkeyInterceptionFactory
    {
        public IHotkeyInterception CreateInterception(
            int hotkey, bool controlNeeded, bool noRepeat)
        {
            return new HotkeyInterception()
            {
                ControlNeeded = controlNeeded,
                NoRepeat = noRepeat,
                KeyCode = hotkey
            };
        }
    }
}
