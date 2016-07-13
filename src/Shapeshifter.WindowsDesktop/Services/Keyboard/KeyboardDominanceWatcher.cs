namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;

    using Stability.Interfaces;

    public class KeyboardDominanceWatcher : IKeyboardDominanceWatcher
    {

        public KeyboardDominanceWatcher()
        {
        }

        public event EventHandler KeyboardAccessOverruled;
        public event EventHandler KeyboardAccessRestored;

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnKeyboardAccessOverruled()
        {
            KeyboardAccessOverruled?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnKeyboardAccessRestored()
        {
            KeyboardAccessRestored?.Invoke(this, EventArgs.Empty);
        }
    }
}