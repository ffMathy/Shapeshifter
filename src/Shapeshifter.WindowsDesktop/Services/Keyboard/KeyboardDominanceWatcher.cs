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
    }
}