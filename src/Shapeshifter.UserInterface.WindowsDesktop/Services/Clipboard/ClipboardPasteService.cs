using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard
{
    class ClipboardPasteService : IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
        }

        public void PasteClipboardContents()
        {
            pasteHotkeyInterceptor.Disable();
            SendPasteCombination();
            pasteHotkeyInterceptor.Enable();
        }

        void SendPasteCombination()
        {
            throw new NotImplementedException();
        }
    }
}
