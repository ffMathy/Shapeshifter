using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events
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