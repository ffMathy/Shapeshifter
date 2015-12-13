namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories
{
    using Hotkeys.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    class HotkeyInterceptionFactory: IHotkeyInterceptionFactory
    {
        readonly IKeyboardNativeApi keyboardNativeApi;

        public HotkeyInterceptionFactory(
            IKeyboardNativeApi keyboardNativeApi)
        {
            this.keyboardNativeApi = keyboardNativeApi;
        }

        public IHotkeyInterception CreateInterception(
            int hotkey,
            bool controlNeeded,
            bool noRepeat)
        {
            return new HotkeyInterception(keyboardNativeApi)
            {
                ControlNeeded = controlNeeded,
                NoRepeat = noRepeat,
                KeyCode = hotkey
            };
        }
    }
}