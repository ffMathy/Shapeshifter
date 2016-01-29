namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;

    using Infrastructure.Events;

    using Interfaces;

    public class KeyboardHook: IKeyboardHook
    {
        public bool IsConnected { get; }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<KeyDetectedArgument> KeyDetected;

        protected virtual void OnKeyDetected(KeyDetectedArgument e)
        {
            KeyDetected?.Invoke(this, e);
        }
    }
}