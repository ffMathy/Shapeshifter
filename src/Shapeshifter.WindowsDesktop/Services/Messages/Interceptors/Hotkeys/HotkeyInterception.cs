namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Threading;
    using System.Windows.Input;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    class HotkeyInterception: IHotkeyInterception
    {
        readonly IKeyboardNativeApi keyboardNativeApi;

        static int interceptionId;

        public int InterceptionId { get; }

        public bool ControlKeyNeeded { get; set; }
        public bool WinKeyNeeded { get; set; }
        public bool ShiftKeyNeeded { get; set; }
        public bool AltKeyNeeded { get; set; }

        public bool NoRepeat { get; set; }

        public Key Key { get; set; }

        public HotkeyInterception(
            IKeyboardNativeApi keyboardNativeApi)
        {
            this.keyboardNativeApi = keyboardNativeApi;
            InterceptionId = Interlocked.Increment(ref interceptionId);
        }

        public void Start(IntPtr windowHandle)
        {
            UnregisterHotkey(windowHandle);

            var modifier = 0;

            if (ControlKeyNeeded)
            {
                modifier |= KeyboardNativeApi.MOD_CONTROL;
            }
            if (AltKeyNeeded)
            {
                modifier |= KeyboardNativeApi.MOD_ALT;
            }
            if (ShiftKeyNeeded)
            {
                modifier |= KeyboardNativeApi.MOD_SHIFT;
            }
            if (WinKeyNeeded)
            {
                modifier |= KeyboardNativeApi.MOD_WIN;
            }

            if (NoRepeat)
            {
                modifier |= KeyboardNativeApi.MOD_NOREPEAT;
            }

            InstallHotkey(windowHandle, modifier, Key);
        }

        void InstallHotkey(IntPtr windowHandle, int modifier, Key key)
        {
            var keyCode = KeyInterop.VirtualKeyFromKey(key);
            var registrationResult = keyboardNativeApi.RegisterHotKey(
                windowHandle,
                InterceptionId,
                modifier,
                keyCode);
            if (!registrationResult)
            {
                throw new InvalidOperationException(
                    $"Couldn't install the hotkey interceptor for key {Key}.");
            }
        }

        public void Stop(IntPtr windowHandle)
        {
            var registrationResult = UnregisterHotkey(windowHandle);
            if (!registrationResult)
            {
                throw new InvalidOperationException(
                    $"Couldn't uninstall the hotkey interceptor for key {Key}.");
            }
        }

        bool UnregisterHotkey(IntPtr windowHandle)
        {
            return keyboardNativeApi.UnregisterHotKey(
                windowHandle,
                InterceptionId);
        }
    }
}