namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories
{
    using Hotkeys.Interfaces;

    using Interfaces;

    class HotkeyInterceptionFactory: IHotkeyInterceptionFactory
    {
        public IHotkeyInterception CreateInterception(
            int hotkey,
            bool controlNeeded,
            bool noRepeat)
        {
            return new HotkeyInterception
            {
                ControlNeeded = controlNeeded,
                NoRepeat = noRepeat,
                KeyCode = hotkey
            };
        }
    }
}