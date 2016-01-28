namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;
    using System.Windows.Input;

    public class KeyDetectedArgument: EventArgs
    {
        public KeyDetectedArgument(Key key, bool isControlDown)
        {
            Key = key;
            IsControlDown = isControlDown;
        }

        public bool Cancel { get; set; }

        public bool IsControlDown { get; private set; }

        public Key Key { get; private set; }
    }
}