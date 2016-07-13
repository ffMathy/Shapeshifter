namespace Shapeshifter.WindowsDesktop.Services.Stability.Interfaces
{
    using System;

    using Infrastructure.Dependencies.Interfaces;

    public interface IKeyboardDominanceWatcher: ISingleInstance
    {
        event EventHandler KeyboardAccessOverruled;
        event EventHandler KeyboardAccessRestored;

        void Start();
        void Stop();
    }
}