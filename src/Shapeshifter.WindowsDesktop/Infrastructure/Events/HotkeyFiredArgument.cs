namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    public class HotkeyFiredArgument: EventArgs
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