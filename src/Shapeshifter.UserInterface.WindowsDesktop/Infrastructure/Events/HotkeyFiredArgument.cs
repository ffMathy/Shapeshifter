using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    public class HotkeyFiredArgument : EventArgs
    {
        public HotkeyFiredArgument(int keyCode, bool isControlDown)
        {
            KeyCode = keyCode;
            IsControlDown = isControlDown;
        }

        public bool IsControlDown { get; private set; }

        public int KeyCode { get; private set; }
    }
}
