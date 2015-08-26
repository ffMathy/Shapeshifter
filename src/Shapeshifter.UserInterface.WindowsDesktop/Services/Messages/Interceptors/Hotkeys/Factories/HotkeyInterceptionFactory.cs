using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories
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
                Hotkey = hotkey
            };
        }
    }
}
