namespace
    Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.
    Interfaces
{
    using Hotkeys.Interfaces;

    public interface IHotkeyInterceptionFactory
    {
        IHotkeyInterception CreateInterception(
            int hotkey,
            bool controlNeeded,
            bool noRepeat);
    }
}