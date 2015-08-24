using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using static Shapeshifter.UserInterface.WindowsDesktop.Services.Api.KeyboardApi;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardPasteService : IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly IWindowMessageHook windowMessageHook;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IWindowMessageHook windowMessageHook)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.windowMessageHook = windowMessageHook;
        }

        public void PasteClipboardContents()
        {
            DisablePasteHotkeyInterceptor();
            SendPasteCombination();
            EnablePasteHotkeyInterceptor();
        }

        void EnablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Install(windowMessageHook.MainWindowHandle);
        }

        void DisablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Uninstall(windowMessageHook.MainWindowHandle);
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
