namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Keyboard;
    using Keyboard.Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    using Native;
    using Native.Interfaces;

    class ClipboardPasteService: IClipboardPasteService
    {
        const int KeyboardGracePeriodMilliseconds = 100;

        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly ILogger logger;
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyboardManager keyboardManager;
        readonly IThreadDelay delay;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IMainWindowHandleContainer handleContainer,
            IKeyboardManager keyboardManager,
            IThreadDelay delay)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.handleContainer = handleContainer;
            this.keyboardManager = keyboardManager;
            this.delay = delay;
        }

        public void PasteClipboardContents()
        {
            DisablePasteHotkeyInterceptor();

            delay.Execute(KeyboardGracePeriodMilliseconds);

            SendPasteCombination();

            delay.Execute(KeyboardGracePeriodMilliseconds);

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
            keyboardManager.SendKeys(
                new KeyOperation(Key.LeftCtrl, KeyDirection.Down),
                new KeyOperation(Key.V, KeyDirection.Down),
                new KeyOperation(Key.V, KeyDirection.Up),
                new KeyOperation(Key.LeftCtrl, KeyDirection.Up));
        }
    }
}