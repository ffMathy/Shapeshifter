using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.Interfaces
{
    public interface IHotkeyInterceptionFactory
    {
        IHotkeyInterception CreateInterception(
            int hotkey, bool controlNeeded, bool noRepeat);
    }
}
