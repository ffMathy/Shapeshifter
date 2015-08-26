using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Factories.Interfaces
{
    public interface IHotkeyInterceptionFactory
    {
        IHotkeyInterception CreateInterception(
            int hotkey, bool controlNeeded, bool noRepeat);
    }
}
