namespace
    Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories.
    Interfaces
{
    using System.Windows.Input;

    using Hotkeys.Interfaces;

    public interface IHotkeyInterceptionFactory
    {
        IHotkeyInterception CreateInterception(
            Key hotkey,
            bool controlNeeded,
            bool noRepeat);
    }
}