using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Logging.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using static Shapeshifter.UserInterface.WindowsDesktop.Services.Api.KeyboardApi;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardPasteService : IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly ILogger logger;
        readonly IClipboardListWindow mainWindow;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IClipboardListWindow mainWindow)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.mainWindow = mainWindow;
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
            pasteHotkeyInterceptor.Install(mainWindow.HandleSource.Handle);
        }

        void DisablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Uninstall(mainWindow.HandleSource.Handle);
        }

        void SendPasteCombination()
        {
            var inputs = new[]
            {
                GenerateKeystoke(VirtualKeyShort.LCONTROL),
                GenerateKeystoke(VirtualKeyShort.KEY_V),
                GenerateKeystoke(VirtualKeyShort.KEY_V, KEYEVENTF.KEYUP),
                GenerateKeystoke(VirtualKeyShort.LCONTROL, KEYEVENTF.KEYUP)
            };
            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        static INPUT GenerateKeystoke(VirtualKeyShort key, KEYEVENTF flags = 0)
        {
            return new INPUT()
            {
                type = 1,
                U = new InputUnion()
                {
                    ki = new KEYBDINPUT()
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
