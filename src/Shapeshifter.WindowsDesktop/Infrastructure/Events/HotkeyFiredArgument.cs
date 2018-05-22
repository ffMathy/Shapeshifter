namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;
    using System.Windows.Input;

    public class HotkeyFiredArgument: EventArgs
    {
        public HotkeyFiredArgument(Key key, bool isControlDown)
        {
            Key = key;
            IsControlDown = isControlDown;
        }

        public bool IsControlDown { get; }

        public Key Key { get; }
    }
}