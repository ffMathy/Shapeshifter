namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Threading;

    using Controls.Window.Interfaces;

    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    using Native;
    using Native.Interfaces;

    class ClipboardPasteService: IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        readonly ILogger logger;

        readonly IMainWindowHandleContainer handleContainer;

        readonly IKeyboardNativeApi keyboardNativeApi;

        readonly IThreadDelay delay;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IMainWindowHandleContainer handleContainer,
            IKeyboardNativeApi keyboardNativeApi,
            IThreadDelay delay)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.handleContainer = handleContainer;
            this.keyboardNativeApi = keyboardNativeApi;
            this.delay = delay;
        }

        public void PasteClipboardContents()
        {
            DisablePasteHotkeyInterceptor();
            SendPasteCombination();
            EnablePasteHotkeyInterceptor();

            logger.Information("Paste simulated.", 1);
        }

        void EnablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Install(handleContainer.Handle);
        }

        void DisablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Uninstall();
        }

        void SendPasteCombination()
        {
            delay.Execute(100);

            var inputs = new[]
            {
                GenerateKeystoke(KeyboardNativeApi.VirtualKeyShort.LCONTROL),
                GenerateKeystoke(KeyboardNativeApi.VirtualKeyShort.KEY_V),
                GenerateKeystoke(KeyboardNativeApi.VirtualKeyShort.KEY_V, KeyboardNativeApi.KEYEVENTF.KEYUP),
                GenerateKeystoke(KeyboardNativeApi.VirtualKeyShort.LCONTROL, KeyboardNativeApi.KEYEVENTF.KEYUP)
            };
            keyboardNativeApi.SendInput((uint) inputs.Length, inputs, KeyboardNativeApi.INPUT.Size);
        }

        static KeyboardNativeApi.INPUT GenerateKeystoke(KeyboardNativeApi.VirtualKeyShort key, KeyboardNativeApi.KEYEVENTF flags = 0)
        {
            return new KeyboardNativeApi.INPUT
            {
                type = 1,
                U = new KeyboardNativeApi.InputUnion
                {
                    ki = new KeyboardNativeApi.KEYBDINPUT
                    {
                        wVk = key,
                        dwFlags = flags,
                        wScan = 0
                    }
                }
            };
        }
    }
}