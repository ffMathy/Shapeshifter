namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Factories
{
    using System.Windows.Input;

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
            Key hotkey,
            bool controlNeeded,
            bool noRepeat)
        {
            return new HotkeyInterception(keyboardNativeApi)
            {
                ControlKeyNeeded = controlNeeded,
                NoRepeat = noRepeat,
                Key = hotkey
            };
        }
    }
}