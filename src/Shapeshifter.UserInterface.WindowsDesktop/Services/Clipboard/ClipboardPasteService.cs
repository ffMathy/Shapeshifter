using static Shapeshifter.UserInterface.WindowsDesktop.Api.KeyboardApi;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    using Controls.Window.Interfaces;

    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    class ClipboardPasteService: IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        readonly ILogger logger;

        readonly IMainWindowHandleContainer handleContainer;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IMainWindowHandleContainer handleContainer)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.handleContainer = handleContainer;
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

        
        static void SendPasteCombination()
        {
            var inputs = new[]
            {
                GenerateKeystoke(VirtualKeyShort.LCONTROL),
                GenerateKeystoke(VirtualKeyShort.KEY_V),
                GenerateKeystoke(VirtualKeyShort.KEY_V, KEYEVENTF.KEYUP),
                GenerateKeystoke(VirtualKeyShort.LCONTROL, KEYEVENTF.KEYUP)
            };
            SendInput((uint) inputs.Length, inputs, INPUT.Size);
        }

        
        static INPUT GenerateKeystoke(VirtualKeyShort key, KEYEVENTF flags = 0)
        {
            return new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
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